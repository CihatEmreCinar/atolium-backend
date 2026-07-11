using CommunityPlatform.Application.DTOs.Social;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Domain.Enums;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CommunityPlatform.Infrastructure.Services;

public class SocialService(
    AppDbContext db,
    ICurrentUserService currentUser,
    IConfiguration config,
    INotificationService notifications)
{
    // ─── Like ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Idempotent toggle — zaten like varsa kaldır, yoksa ekle.
    /// LikeCount trigger tarafından sync edilir; burada sadece kayıt yönetilir.
    /// </summary>
    public async Task<LikeResponse> ToggleLikeAsync(Guid postId)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var post = await db.Posts
            .Include(p => p.Employer)
            .Include(p => p.Cafe)
            .FirstOrDefaultAsync(p => p.Id == postId)
            ?? throw new KeyNotFoundException("Post bulunamadı.");

        var existing = await db.PostLikes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

        bool nowLiked;

        if (existing is not null)
        {
            db.PostLikes.Remove(existing);
            await db.SaveChangesAsync();
            nowLiked = false;
        }
        else
        {
            db.PostLikes.Add(new PostLike { PostId = postId, UserId = userId });
            await db.SaveChangesAsync();
            nowLiked = true;
        }

        var likeCount = await db.PostLikes.CountAsync(l => l.PostId == postId);

        // Bildirim: like atılınca post sahibine gönder (Employer ya da Cafe).
        // Kendi postuna like atan kullanıcıya ve unlike durumunda gönderme.
        var likeOwnerUserId = post.AuthorType == PostAuthorType.Cafe ? post.Cafe?.UserId : post.Employer?.UserId;

        if (nowLiked && likeOwnerUserId != null && likeOwnerUserId != userId)
        {
            var liker = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.FirstName, u.LastName })
                .FirstOrDefaultAsync();

            await notifications.NotifyAsync(
                userId:    likeOwnerUserId.Value,
                type:      NotificationType.PostLiked,
                title:     "Gönderiniz beğenildi",
                body:      $"{liker?.FirstName} {liker?.LastName} gönderinizi beğendi.",
                metadata:  new { postId, likedByUserId = userId },
                sendEmail: false);
        }

        return new LikeResponse
        {
            IsLiked   = nowLiked,
            LikeCount = likeCount
        };
    }

    // ─── Comment ─────────────────────────────────────────────────────────────

    public async Task<CommentListResponse> GetCommentsAsync(
        Guid postId,
        string? cursor,
        int limit = 20)
    {
        _ = await db.Posts.FirstOrDefaultAsync(p => p.Id == postId)
            ?? throw new KeyNotFoundException("Post bulunamadı.");

        // Sadece top-level yorumları getir (ParentCommentId == null)
        var query = db.PostComments
            .Where(c => c.PostId == postId && c.ParentCommentId == null)
            .Include(c => c.Author)
            .Include(c => c.Replies).ThenInclude(r => r.Author)
            .AsNoTracking();

        // Cursor: createdAt|id formatı
        if (!string.IsNullOrEmpty(cursor))
        {
            var (cursorDate, cursorId) = DecodeCursor(cursor);
            query = query.Where(c =>
                c.CreatedAt < cursorDate ||
                (c.CreatedAt == cursorDate && c.Id.CompareTo(cursorId) < 0));
        }

        var comments = await query
            .OrderByDescending(c => c.CreatedAt)
            .Take(limit + 1)
            .ToListAsync();

        var hasNext = comments.Count > limit;
        if (hasNext) comments.RemoveAt(comments.Count - 1);

        return new CommentListResponse
        {
            Comments    = comments.Select(MapComment).ToList(),
            HasNextPage = hasNext,
            NextCursor  = hasNext
                ? EncodeCursor(comments.Last().CreatedAt, comments.Last().Id)
                : null
        };
    }

    public async Task<CommentResponse> CreateCommentAsync(Guid postId, CreateCommentRequest req)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var post = await db.Posts
            .Include(p => p.Employer)
            .Include(p => p.Cafe)
            .FirstOrDefaultAsync(p => p.Id == postId)
            ?? throw new KeyNotFoundException("Post bulunamadı.");

        // 1-level limit: reply'ın reply'ına izin verme
        if (req.ParentCommentId.HasValue)
        {
            var parent = await db.PostComments
                .FirstOrDefaultAsync(c => c.Id == req.ParentCommentId && c.PostId == postId)
                ?? throw new KeyNotFoundException("Parent yorum bulunamadı.");

            if (parent.ParentCommentId.HasValue)
                throw new InvalidOperationException("Maksimum 1 seviye nested yorum destekleniyor.");
        }

        var comment = new PostComment
        {
            PostId          = postId,
            AuthorId        = userId,
            ParentCommentId = req.ParentCommentId,
            Content         = req.Content
        };

        db.PostComments.Add(comment);
        await db.SaveChangesAsync();

        // Response için yeniden yükle (Author navigation lazım)
        var loaded = await db.PostComments
            .Include(c => c.Author)
            .Include(c => c.Replies).ThenInclude(r => r.Author)
            .AsNoTracking()
            .FirstAsync(c => c.Id == comment.Id);

        // Bildirim: post sahibine yorum geldi (Employer ya da Cafe).
        // Kendi postuna yorum yapan kullanıcıya ve reply durumunda parent yorum sahibine de gönder.
        var commentOwnerUserId = post.AuthorType == PostAuthorType.Cafe ? post.Cafe?.UserId : post.Employer?.UserId;

        if (commentOwnerUserId != null && commentOwnerUserId != userId)
        {
            var commenter = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.FirstName, u.LastName })
                .FirstOrDefaultAsync();

            await notifications.NotifyAsync(
                userId:    commentOwnerUserId.Value,
                type:      NotificationType.PostCommented,
                title:     "Yeni yorum",
                body:      $"{commenter?.FirstName} {commenter?.LastName} gönderinize yorum yaptı: \"{Truncate(req.Content)}\"",
                metadata:  new { postId, commentId = comment.Id, commentedByUserId = userId },
                sendEmail: false);
        }

        // Reply bildirimi: parent yorum sahibine ayrıca bildir
        // (post sahibiyle aynı kişiyse zaten üstte bildirim gitti, tekrar gönderme)
        if (req.ParentCommentId.HasValue)
        {
            var parentComment = await db.PostComments
                .AsNoTracking()
                .Where(c => c.Id == req.ParentCommentId)
                .Select(c => new { c.AuthorId })
                .FirstOrDefaultAsync();

            if (parentComment is not null
                && parentComment.AuthorId != userId
                && parentComment.AuthorId != commentOwnerUserId)
            {
                var commenter = await db.Users
                    .AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.FirstName, u.LastName })
                    .FirstOrDefaultAsync();

                await notifications.NotifyAsync(
                    userId:    parentComment.AuthorId,
                    type:      NotificationType.PostCommented,
                    title:     "Yorumunuza yanıt geldi",
                    body:      $"{commenter?.FirstName} {commenter?.LastName} yorumunuzu yanıtladı: \"{Truncate(req.Content)}\"",
                    metadata:  new { postId, commentId = comment.Id, parentCommentId = req.ParentCommentId, repliedByUserId = userId },
                    sendEmail: false);
            }
        }

        return MapComment(loaded);
    }

    public async Task DeleteCommentAsync(Guid commentId)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var comment = await db.PostComments
            .FirstOrDefaultAsync(c => c.Id == commentId)
            ?? throw new KeyNotFoundException("Yorum bulunamadı.");

        if (comment.AuthorId != userId)
            throw new UnauthorizedAccessException("Bu yorumu silme yetkiniz yok.");

        db.PostComments.Remove(comment);
        await db.SaveChangesAsync();
    }

    // ─── Follow / Unfollow ───────────────────────────────────────────────────

    public async Task<FollowResponse> ToggleFollowAsync(Guid targetUserId)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        if (userId == targetUserId)
            throw new InvalidOperationException("Kendinizi takip edemezsiniz.");

        _ = await db.Users.FirstOrDefaultAsync(u => u.Id == targetUserId)
            ?? throw new KeyNotFoundException("Kullanıcı bulunamadı.");

        var existing = await db.UserFollows
            .FirstOrDefaultAsync(f => f.FollowerId == userId && f.FollowedId == targetUserId);

        bool nowFollowing;

        if (existing is not null)
        {
            db.UserFollows.Remove(existing);
            await db.SaveChangesAsync();
            nowFollowing = false;
        }
        else
        {
            db.UserFollows.Add(new UserFollow { FollowerId = userId, FollowedId = targetUserId });
            await db.SaveChangesAsync();
            nowFollowing = true;
        }

        var followerCount = await db.UserFollows.CountAsync(f => f.FollowedId == targetUserId);

        // Bildirim: follow edilince hedef kullanıcıya gönder, unfollow'da gönderme.
        if (nowFollowing)
        {
            var follower = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.FirstName, u.LastName })
                .FirstOrDefaultAsync();

            await notifications.NotifyAsync(
                userId:    targetUserId,
                type:      NotificationType.NewFollower,
                title:     "Yeni takipçi",
                body:      $"{follower?.FirstName} {follower?.LastName} sizi takip etmeye başladı.",
                metadata:  new { followerId = userId },
                sendEmail: false);
        }

        return new FollowResponse
        {
            IsFollowing   = nowFollowing,
            FollowerCount = followerCount
        };
    }

    public async Task<FollowListResponse> GetFollowersAsync(Guid userId, string? cursor, int limit = 20)
    {
        var query = db.UserFollows
            .Where(f => f.FollowedId == userId)
            .Include(f => f.Follower)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(cursor))
        {
            var (cursorDate, cursorId) = DecodeCursor(cursor);
            query = query.Where(f =>
                f.CreatedAt < cursorDate ||
                (f.CreatedAt == cursorDate && f.Id.CompareTo(cursorId) < 0));
        }

        var follows = await query
            .OrderByDescending(f => f.CreatedAt)
            .Take(limit + 1)
            .ToListAsync();

        var hasNext = follows.Count > limit;
        if (hasNext) follows.RemoveAt(follows.Count - 1);

        var currentUserId = currentUser.UserId;
        var followerIds   = follows.Select(f => f.FollowerId).ToList();

        // Mevcut kullanıcının kimleri takip ettiğini toplu çek
        var followingBack = currentUserId != null
            ? await db.UserFollows
                .Where(f => f.FollowerId == currentUserId && followerIds.Contains(f.FollowedId))
                .Select(f => f.FollowedId)
                .ToHashSetAsync()
            : new HashSet<Guid>();

        return new FollowListResponse
        {
            Users = follows.Select(f => new FollowUserResponse
            {
                UserId          = f.FollowerId,
                Name            = f.Follower.FirstName + " " + f.Follower.LastName,
                AvatarUrl       = f.Follower.AvatarUrl,
                IsFollowingBack = followingBack.Contains(f.FollowerId)
            }).ToList(),
            HasNextPage = hasNext,
            NextCursor  = hasNext
                ? EncodeCursor(follows.Last().CreatedAt, follows.Last().Id)
                : null
        };
    }

    public async Task<FollowListResponse> GetFollowingAsync(Guid userId, string? cursor, int limit = 20)
    {
        var query = db.UserFollows
            .Where(f => f.FollowerId == userId)
            .Include(f => f.Followed)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(cursor))
        {
            var (cursorDate, cursorId) = DecodeCursor(cursor);
            query = query.Where(f =>
                f.CreatedAt < cursorDate ||
                (f.CreatedAt == cursorDate && f.Id.CompareTo(cursorId) < 0));
        }

        var follows = await query
            .OrderByDescending(f => f.CreatedAt)
            .Take(limit + 1)
            .ToListAsync();

        var hasNext = follows.Count > limit;
        if (hasNext) follows.RemoveAt(follows.Count - 1);

        return new FollowListResponse
        {
            Users = follows.Select(f => new FollowUserResponse
            {
                UserId          = f.FollowedId,
                Name            = f.Followed.FirstName + " " + f.Followed.LastName,
                AvatarUrl       = f.Followed.AvatarUrl,
                IsFollowingBack = false
            }).ToList(),
            HasNextPage = hasNext,
            NextCursor  = hasNext
                ? EncodeCursor(follows.Last().CreatedAt, follows.Last().Id)
                : null
        };
    }

    // ─── Share ───────────────────────────────────────────────────────────────

    public async Task<ShareResponse> GetOrCreateShareAsync(Guid postId)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var post = await db.Posts
            .Include(p => p.Employer)
            .Include(p => p.Cafe)
            .FirstOrDefaultAsync(p => p.Id == postId)
            ?? throw new KeyNotFoundException("Post bulunamadı.");

        var existing = await db.PostShares
            .FirstOrDefaultAsync(s => s.PostId == postId && s.SharedById == userId);

        var token = existing?.ShareToken ?? Guid.NewGuid();

        if (existing is null)
        {
            db.PostShares.Add(new PostShare
            {
                PostId     = postId,
                SharedById = userId,
                ShareToken = token
            });
            await db.SaveChangesAsync();

            // Bildirim: post sahibine paylaşım bildirimi gönder (kendi postunu paylaşırsa gönderme)
            var shareOwnerUserId = post.AuthorType == PostAuthorType.Cafe ? post.Cafe?.UserId : post.Employer?.UserId;

            if (shareOwnerUserId != null && shareOwnerUserId != userId)
            {
                var sharer = await db.Users
                    .AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.FirstName, u.LastName })
                    .FirstOrDefaultAsync();

                await notifications.NotifyAsync(
                    userId:    shareOwnerUserId.Value,
                    type:      NotificationType.PostShared,
                    title:     "Gönderiniz paylaşıldı",
                    body:      $"{sharer?.FirstName} {sharer?.LastName} gönderinizi paylaştı.",
                    metadata:  new { postId, sharedByUserId = userId, shareToken = token },
                    sendEmail: false);
            }
        }

        var baseUrl = config["App:BaseUrl"] ?? "https://atolium.app";
        return new ShareResponse
        {
            ShareToken = token,
            ShareUrl   = $"{baseUrl}/share/{token}"
        };
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static CommentResponse MapComment(PostComment c) => new()
    {
        Id             = c.Id,
        PostId         = c.PostId,
        AuthorId       = c.AuthorId,
        AuthorName     = c.Author.FirstName + " " + c.Author.LastName,
        AuthorAvatarUrl = c.Author.AvatarUrl,
        ParentCommentId = c.ParentCommentId,
        Content        = c.Content,
        LikeCount      = c.LikeCount,
        CreatedAt      = c.CreatedAt,
        Replies        = c.Replies?.Select(MapComment).ToList() ?? new()
    };

    private static string Truncate(string text, int max = 60)
        => text.Length <= max ? text : text[..max] + "…";

    private static string EncodeCursor(DateTime date, Guid id)
    {
        var raw = $"{date:O}|{id}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
    }

    private static (DateTime date, Guid id) DecodeCursor(string cursor)
    {
        var raw   = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
        var parts = raw.Split('|');
        return (DateTime.Parse(parts[0]), Guid.Parse(parts[1]));
    }
}