using CommunityPlatform.Application.Common;
using CommunityPlatform.Application.DTOs.Posts;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Domain.Enums;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CommunityPlatform.Infrastructure.Services;

public class PostService(
    AppDbContext db,
    IStorageProvider storage,
    ICurrentUserService currentUser,
    Microsoft.Extensions.Logging.ILogger<PostService> logger)
{
    // ─── Post CRUD ───────────────────────────────────────────────────────────

    public async Task<PostResponse> CreatePostAsync(CreatePostRequest req)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException("Kullanıcı kimliği bulunamadı (token/sub claim eksik).");

        logger.LogInformation("CreatePostAsync: userId={UserId}, workshopId={WorkshopId}", userId, req.WorkshopId);

        var employerProfile = await db.EmployerProfiles
            .FirstOrDefaultAsync(e => e.UserId == userId);

        Post post;

        if (employerProfile != null)
        {
            // ── Mevcut Employer akışı — DOKUNULMADI ──────────────────────────
            _ = await db.Workshops
                .FirstOrDefaultAsync(w => w.Id == req.WorkshopId && w.EmployerId == employerProfile.UserId)
                ?? throw new KeyNotFoundException("Workshop bulunamadı veya bu employer'a ait değil.");

            post = new Post
            {
                EmployerId = employerProfile.Id,
                WorkshopId = req.WorkshopId,
                CafeId = null,
                AuthorType = PostAuthorType.Employer,
                Visibility = PostVisibility.Public,
                Caption = req.Caption,
            };
        }
        else
        {
            // ── Yeni Cafe akışı ───────────────────────────────────────────────
            var cafeProfile = await db.CafeProfiles
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cafeProfile == null)
            {
                logger.LogWarning("CreatePostAsync: Ne EmployerProfile ne CafeProfile bulunamadı, userId={UserId}", userId);
                throw new UnauthorizedAccessException("Employer veya Cafe profili bulunamadı.");
            }

            post = new Post
            {
                EmployerId = null,
                WorkshopId = null,
                CafeId = cafeProfile.Id,
                AuthorType = PostAuthorType.Cafe,
                // Cafe post'u HER ZAMAN EmployersOnly — request'ten gelen değer kullanılmaz.
                Visibility = PostVisibility.EmployersOnly,
                Caption = req.Caption,
            };
        }

        db.Posts.Add(post);
        await db.SaveChangesAsync();

        if (req.TagSlugs.Count > 0)
            await AttachTagsAsync(post.Id, req.TagSlugs);

        return await GetPostByIdAsync(post.Id)
            ?? throw new Exception("Post oluşturuldu ancak getirilemedi.");
    }

    public async Task<PostResponse?> GetPostByIdAsync(Guid postId)
    {
        var userId = currentUser.UserId;

        var post = await db.Posts
            .Include(p => p.Employer).ThenInclude(e => e!.User)
            .Include(p => p.Workshop)
            .Include(p => p.Cafe)
            .Include(p => p.Media.Where(m => m.ConfirmedAt != null))
            .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
            .Include(p => p.Likes)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null) return null;

        // EmployersOnly bir Cafe post'una feed dışından (direkt link/ID) erişimi de
        // engelle — aksi halde feed'deki visibility filtresi anlamsızlaşır.
        var canSeeEmployersOnly = currentUser.Role == "employer" || currentUser.Role == "cafe";
        if (post.Visibility == PostVisibility.EmployersOnly && !canSeeEmployersOnly)
            return null;

        // NOT: post.Employer.UserId'ye direkt erişmek Cafe post'larında NullReferenceException
        // fırlatırdı — AuthorType'a göre doğru sahibi (Employer ya da Cafe) seç.
        var ownerUserId = post.AuthorType == PostAuthorType.Cafe
            ? post.Cafe?.UserId
            : post.Employer?.UserId;

        var isFollowing = userId != null && ownerUserId != null && await db.UserFollows
            .AnyAsync(f => f.FollowerId == userId && f.FollowedId == ownerUserId.Value);

        return MapToResponse(post, userId, isFollowing);
    }

    public async Task<PostResponse> UpdatePostAsync(Guid postId, UpdatePostRequest req)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException("Kullanıcı kimliği bulunamadı (token/sub claim eksik).");

        var employerProfile = await db.EmployerProfiles
            .FirstOrDefaultAsync(e => e.UserId == userId);

        Post post;

        if (employerProfile != null)
        {
            post = await db.Posts
                .Include(p => p.PostTags)
                .FirstOrDefaultAsync(p => p.Id == postId && p.EmployerId == employerProfile.Id)
                ?? throw new KeyNotFoundException("Post bulunamadı.");
        }
        else
        {
            var cafeProfile = await db.CafeProfiles
                .FirstOrDefaultAsync(c => c.UserId == userId)
                ?? throw new UnauthorizedAccessException();

            post = await db.Posts
                .Include(p => p.PostTags)
                .FirstOrDefaultAsync(p => p.Id == postId && p.CafeId == cafeProfile.Id)
                ?? throw new KeyNotFoundException("Post bulunamadı.");
        }

        post.Caption = req.Caption;
        post.UpdatedAt = DateTime.UtcNow;

        db.PostTags.RemoveRange(post.PostTags);
        await db.SaveChangesAsync();

        if (req.TagSlugs.Count > 0)
            await AttachTagsAsync(post.Id, req.TagSlugs);

        return await GetPostByIdAsync(postId)
            ?? throw new Exception("Post güncellenemedi.");
    }

    public async Task DeletePostAsync(Guid postId)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException("Kullanıcı kimliği bulunamadı (token/sub claim eksik).");

        var employerProfile = await db.EmployerProfiles
            .FirstOrDefaultAsync(e => e.UserId == userId);

        Post post;

        if (employerProfile != null)
        {
            post = await db.Posts
                .Include(p => p.Media)
                .FirstOrDefaultAsync(p => p.Id == postId && p.EmployerId == employerProfile.Id)
                ?? throw new KeyNotFoundException("Post bulunamadı.");
        }
        else
        {
            var cafeProfile = await db.CafeProfiles
                .FirstOrDefaultAsync(c => c.UserId == userId)
                ?? throw new UnauthorizedAccessException();

            post = await db.Posts
                .Include(p => p.Media)
                .FirstOrDefaultAsync(p => p.Id == postId && p.CafeId == cafeProfile.Id)
                ?? throw new KeyNotFoundException("Post bulunamadı.");
        }

        foreach (var media in post.Media)
            await storage.DeleteAsync(media.StorageKey);

        db.Posts.Remove(post);
        await db.SaveChangesAsync();
    }

    // ─── Media Upload ────────────────────────────────────────────────────────

    public async Task<UploadMediaResponse> PrepareMediaAsync(
        Guid postId,
        IFormFile file,
        short orderIndex)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException("Kullanıcı kimliği bulunamadı (token/sub claim eksik).");

        var employerProfile = await db.EmployerProfiles
            .FirstOrDefaultAsync(e => e.UserId == userId);

        if (employerProfile != null)
        {
            _ = await db.Posts
                .FirstOrDefaultAsync(p => p.Id == postId && p.EmployerId == employerProfile.Id)
                ?? throw new KeyNotFoundException("Post bulunamadı.");
        }
        else
        {
            var cafeProfile = await db.CafeProfiles
                .FirstOrDefaultAsync(c => c.UserId == userId)
                ?? throw new UnauthorizedAccessException();

            _ = await db.Posts
                .FirstOrDefaultAsync(p => p.Id == postId && p.CafeId == cafeProfile.Id)
                ?? throw new KeyNotFoundException("Post bulunamadı.");
        }

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "video/mp4" };
        if (!allowedTypes.Contains(file.ContentType))
            throw new ArgumentException("Desteklenmeyen dosya tipi.");

        const long maxSize = 50 * 1024 * 1024;
        if (file.Length > maxSize)
            throw new ArgumentException("Dosya boyutu 50 MB'ı geçemez.");

        if (!FileUploadValidator.TryGetSafeExtension(file.ContentType, allowVideo: true, out var ext))
            throw new ArgumentException("Desteklenmeyen dosya tipi.");
        var key = $"posts/{postId}/{Guid.NewGuid()}{ext}";

        await using var stream = file.OpenReadStream();
        var result = await storage.SaveAsync(key, stream, file.ContentType);

        var mediaType = file.ContentType.StartsWith("video") ? PostMediaType.Video : PostMediaType.Image;

        var media = new PostMedia
        {
            PostId = postId,
            MediaType = mediaType,
            StorageKey = result.Key,
            CdnUrl = result.Url,
            OrderIndex = orderIndex,
            SizeBytes = result.SizeBytes,
            ConfirmedAt = DateTime.UtcNow
        };

        db.PostMedia.Add(media);
        await db.SaveChangesAsync();

        return new UploadMediaResponse
        {
            MediaId = media.Id,
            UploadUrl = result.Url,
            OrderIndex = orderIndex
        };
    }

    public async Task DeleteMediaAsync(Guid postId, Guid mediaId)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException("Kullanıcı kimliği bulunamadı (token/sub claim eksik).");

        var employerProfile = await db.EmployerProfiles
            .FirstOrDefaultAsync(e => e.UserId == userId);

        PostMedia media;

        if (employerProfile != null)
        {
            media = await db.PostMedia
                .Include(m => m.Post)
                .FirstOrDefaultAsync(m => m.Id == mediaId
                    && m.PostId == postId
                    && m.Post.EmployerId == employerProfile.Id)
                ?? throw new KeyNotFoundException("Medya bulunamadı.");
        }
        else
        {
            var cafeProfile = await db.CafeProfiles
                .FirstOrDefaultAsync(c => c.UserId == userId)
                ?? throw new UnauthorizedAccessException();

            media = await db.PostMedia
                .Include(m => m.Post)
                .FirstOrDefaultAsync(m => m.Id == mediaId
                    && m.PostId == postId
                    && m.Post.CafeId == cafeProfile.Id)
                ?? throw new KeyNotFoundException("Medya bulunamadı.");
        }

        await storage.DeleteAsync(media.StorageKey);
        db.PostMedia.Remove(media);
        await db.SaveChangesAsync();
    }

    // ─── Internal helpers ────────────────────────────────────────────────────

    // FIX: Tek SaveChanges — önce eksik tag'leri bul ve toplu ekle, sonra junction'ları ekle
    private async Task AttachTagsAsync(Guid postId, List<string> slugs)
    {
        var distinctSlugs = slugs.Distinct().ToList();

        var existingTags = await db.Tags
            .Where(t => distinctSlugs.Contains(t.Slug))
            .ToListAsync();

        var existingSlugs = existingTags.Select(t => t.Slug).ToHashSet();

        var newTags = distinctSlugs
            .Where(s => !existingSlugs.Contains(s))
            .Select(s => new Tag { Name = s.Replace("-", " "), Slug = s })
            .ToList();

        if (newTags.Count > 0)
        {
            db.Tags.AddRange(newTags);
            await db.SaveChangesAsync();
        }

        var allTags = existingTags.Concat(newTags).ToList();

        var postTags = allTags.Select(t => new PostTag { PostId = postId, TagId = t.Id });
        db.PostTags.AddRange(postTags);
        await db.SaveChangesAsync();
    }

    // FIX: Static değil, Include + in-memory map
    // isFollowing ayrı parametre — çünkü UserFollow sorgusu burada yapılamaz (static context yok)
    // FIX (Cafe post desteği): p.Employer / p.Workshop artık nullable — AuthorType'a göre
    // hangi taraf doluysa onu oku, null-conditional erişim kullan.
    internal static PostResponse MapToResponse(Post p, Guid? userId, bool isFollowing = false) => new()
    {
        Id = p.Id,

        EmployerId = p.AuthorType == PostAuthorType.Employer ? p.EmployerId : null,
        EmployerName = p.AuthorType == PostAuthorType.Employer && p.Employer != null
            ? p.Employer.User.FirstName + " " + p.Employer.User.LastName
            : null,
        EmployerAvatarUrl = p.AuthorType == PostAuthorType.Employer ? p.Employer?.User.AvatarUrl : null,

        CafeId = p.AuthorType == PostAuthorType.Cafe ? p.CafeId : null,
        CafeName = p.AuthorType == PostAuthorType.Cafe ? p.Cafe?.Name : null,
        CafeAvatarUrl = p.AuthorType == PostAuthorType.Cafe ? p.Cafe?.AvatarUrl : null,

        WorkshopId = p.WorkshopId,
        WorkshopTitle = p.Workshop?.Title,

        AuthorType = p.AuthorType.ToString(),
        Visibility = p.Visibility.ToString(),

        Caption = p.Caption,
        LikeCount = p.LikeCount,
        CommentCount = p.CommentCount,
        ShareCount = p.ShareCount,
        ViewCount = p.ViewCount,
        IsLikedByMe = userId != null && p.Likes.Any(l => l.UserId == userId),
        IsFollowingEmployer = isFollowing,
        Media = p.Media
            .Where(m => m.ConfirmedAt != null)
            .OrderBy(m => m.OrderIndex)
            .Select(m => new PostMediaResponse
            {
                Id = m.Id,
                MediaType = m.MediaType.ToString(),
                Url = m.CdnUrl,
                OrderIndex = m.OrderIndex,
                WidthPx = m.WidthPx,
                HeightPx = m.HeightPx
            }).ToList(),
        Tags = p.PostTags.Select(pt => pt.Tag.Slug).ToList(),
        PublishedAt = p.PublishedAt
    };

    // ─── Kullanıcının postları (cursor pagination) ──────────────────────────────

    public async Task<PostListResponse> GetUserPostsAsync(
        Guid userId,
        string? cursor,
        int limit = 15)
    {
        var employerProfile = await db.EmployerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId);

        IQueryable<Post> query;

        if (employerProfile != null)
        {
            // ── Mevcut Employer akışı — DOKUNULMADI ──────────────────────────
            query = db.Posts.Where(p => p.EmployerId == employerProfile.Id);
        }
        else
        {
            // Employer profili yoksa Cafe profiline bak
            var cafeProfile = await db.CafeProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId);

            // Ne Employer ne Cafe ise (employee kullanıcı) boş liste dön
            if (cafeProfile == null)
            {
                return new PostListResponse
                {
                    Posts = new List<PostResponse>(),
                    NextCursor = null,
                    HasNextPage = false
                };
            }

            query = db.Posts.Where(p => p.CafeId == cafeProfile.Id);
        }

        query = query
            .Include(p => p.Employer).ThenInclude(e => e!.User)
            .Include(p => p.Workshop)
            .Include(p => p.Cafe)
            .Include(p => p.Media.Where(m => m.ConfirmedAt != null))
            .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
            .Include(p => p.Likes)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(cursor))
        {
            var (cursorDate, cursorId) = DecodeCursor(cursor);
            query = query.Where(p =>
                p.PublishedAt < cursorDate ||
                (p.PublishedAt == cursorDate && p.Id.CompareTo(cursorId) < 0));
        }

        var posts = await query
            .OrderByDescending(p => p.PublishedAt)
            .Take(limit + 1)
            .ToListAsync();

        var hasNext = posts.Count > limit;
        if (hasNext) posts.RemoveAt(posts.Count - 1);

        var requestingUserId = currentUser.UserId;
        var isFollowing = requestingUserId != null && await db.UserFollows
            .AnyAsync(f => f.FollowerId == requestingUserId && f.FollowedId == userId);

        return new PostListResponse
        {
            Posts = posts.Select(p => MapToResponse(p, requestingUserId, isFollowing)).ToList(),
            NextCursor = hasNext
                ? EncodeCursor(posts.Last().PublishedAt, posts.Last().Id)
                : null,
            HasNextPage = hasNext
        };
    }

    // ─── Kullanıcı sosyal istatistikleri ─────────────────────────────────────────

    public async Task<UserSocialStats> GetUserSocialStatsAsync(Guid userId)
    {
        var employerProfile = await db.EmployerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId);

        var postCount = employerProfile != null
            ? await db.Posts.CountAsync(p => p.EmployerId == employerProfile.Id)
            : 0;

        var followerCount = await db.UserFollows.CountAsync(f => f.FollowedId == userId);
        var followingCount = await db.UserFollows.CountAsync(f => f.FollowerId == userId);

        var requestingUserId = currentUser.UserId;
        var isFollowedByMe = requestingUserId != null && await db.UserFollows
            .AnyAsync(f => f.FollowerId == requestingUserId && f.FollowedId == userId);

        return new UserSocialStats
        {
            PostCount = postCount,
            FollowerCount = followerCount,
            FollowingCount = followingCount,
            IsFollowedByMe = isFollowedByMe
        };
    }

    // ─── Cursor helpers ──────────────────────────────────────────────────────────

    private static string EncodeCursor(DateTime date, Guid id)
    {
        var raw = $"{date:O}|{id}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
    }

    private static (DateTime date, Guid id) DecodeCursor(string cursor)
    {
        var raw = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
        var parts = raw.Split('|');
        return (DateTime.Parse(parts[0]), Guid.Parse(parts[1]));
    }
}
