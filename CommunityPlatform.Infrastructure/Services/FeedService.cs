using CommunityPlatform.Application.DTOs.Feed;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityPlatform.Infrastructure.Services;

public class FeedService(
    AppDbContext db,
    ICurrentUserService currentUser)
{
    // ─── Following Feed (2.3) ────────────────────────────────────────────────

    /// <summary>
    /// Takip edilen employer'ların postları — engagement_score DESC, cursor-based.
    /// Cursor formatı: base64(score|id)
    /// </summary>
    public async Task<FeedResponse> GetFollowingFeedAsync(FeedRequest req)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        // Takip edilen user ID'lerini çek
        var followedUserIds = await db.UserFollows
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FollowedId)
            .ToListAsync();

        if (followedUserIds.Count == 0)
            return new FeedResponse { Posts = new(), HasNextPage = false };

        // Takip edilen user'ların employer profile ID'leri
        var employerIds = await db.EmployerProfiles
            .Where(e => followedUserIds.Contains(e.UserId))
            .Select(e => e.Id)
            .ToListAsync();

        var query = BuildBaseQuery(employerIds, req.Tags, req.WorkshopId);
        query = ApplyCursor(query, req.Cursor);

        var posts = await FetchPostsAsync(query, req.Limit);
        return await BuildResponseAsync(posts, req.Limit, userId);
    }

    // ─── Explore Feed (2.4) ──────────────────────────────────────────────────

    /// <summary>
    /// Tüm postlar — takip edilmeyenler dahil, engagement_score DESC.
    /// Tag filtresi ve workshop filtresi uygulanabilir.
    /// </summary>
    public async Task<FeedResponse> GetExploreFeedAsync(FeedRequest req)
    {
        var userId = currentUser.UserId;

        var query = BuildBaseQuery(null, req.Tags, req.WorkshopId);
        query = ApplyCursor(query, req.Cursor);

        var posts = await FetchPostsAsync(query, req.Limit);
        return await BuildResponseAsync(posts, req.Limit, userId);
    }

    // ─── Workshop Feed ───────────────────────────────────────────────────────

    /// <summary>Belirli bir workshop'a ait tüm postlar</summary>
    public async Task<FeedResponse> GetWorkshopFeedAsync(Guid workshopId, FeedRequest req)
    {
        var userId = currentUser.UserId;

        var query = db.Posts
            .Where(p => p.WorkshopId == workshopId)
            .Include(p => p.Employer).ThenInclude(e => e.User)
            .Include(p => p.Workshop)
            .Include(p => p.Media.Where(m => m.ConfirmedAt != null))
            .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
            .Include(p => p.Likes)
            .AsNoTracking();

        query = ApplyCursor(query, req.Cursor);

        var posts = await FetchPostsAsync(query, req.Limit);
        return await BuildResponseAsync(posts, req.Limit, userId);
    }

    // ─── Internal builders ───────────────────────────────────────────────────

    private IQueryable<Post> BuildBaseQuery(
        List<Guid>? employerIds,
        List<string> tagSlugs,
        Guid? workshopId)
    {
        var query = db.Posts
            .Include(p => p.Employer).ThenInclude(e => e.User)
            .Include(p => p.Workshop)
            .Include(p => p.Media.Where(m => m.ConfirmedAt != null))
            .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
            .Include(p => p.Likes)
            .AsNoTracking()
            .AsQueryable();

        // Employer filtresi (following feed için)
        if (employerIds is { Count: > 0 })
            query = query.Where(p => employerIds.Contains(p.EmployerId));

        // Workshop filtresi
        if (workshopId.HasValue)
            query = query.Where(p => p.WorkshopId == workshopId.Value);

        // Tag filtresi
        if (tagSlugs.Count > 0)
            query = query.Where(p =>
                p.PostTags.Any(pt => tagSlugs.Contains(pt.Tag.Slug)));

        return query;
    }

    /// <summary>
    /// Keyset pagination:
    /// score DESC, id DESC — aynı score'da id'ye göre tie-break
    /// Cursor = base64("score|id")
    /// </summary>
    private static IQueryable<Post> ApplyCursor(IQueryable<Post> query, string? cursor)
    {
        if (string.IsNullOrEmpty(cursor))
            return query.OrderByDescending(p => p.EngagementScore)
                        .ThenByDescending(p => p.Id);

        var (cursorScore, cursorId) = DecodeFeedCursor(cursor);

        return query
            .Where(p =>
                p.EngagementScore < cursorScore ||
                (p.EngagementScore == cursorScore &&
                 p.Id.CompareTo(cursorId) < 0))
            .OrderByDescending(p => p.EngagementScore)
            .ThenByDescending(p => p.Id);
    }

    private static async Task<List<Post>> FetchPostsAsync(IQueryable<Post> query, int limit)
        => await query.Take(limit + 1).ToListAsync();

    private async Task<FeedResponse> BuildResponseAsync(
        List<Post> posts,
        int limit,
        Guid? userId)
    {
        var hasNext = posts.Count > limit;
        if (hasNext) posts.RemoveAt(posts.Count - 1);

        if (posts.Count == 0)
            return new FeedResponse { Posts = new(), HasNextPage = false };

        // IsFollowing toplu çek — N+1 önle
        var employerUserIds = posts.Select(p => p.Employer.UserId).Distinct().ToList();
        var followingSet = userId != null
            ? await db.UserFollows
                .Where(f => f.FollowerId == userId && employerUserIds.Contains(f.FollowedId))
                .Select(f => f.FollowedId)
                .ToHashSetAsync()
            : new HashSet<Guid>();

        var mapped = posts.Select(p => MapToFeedPost(p, userId, followingSet)).ToList();

        return new FeedResponse
        {
            Posts = mapped,
            HasNextPage = hasNext,
            NextCursor = hasNext
                ? EncodeFeedCursor(posts.Last().EngagementScore, posts.Last().Id)
                : null
        };
    }

    // ─── Mapping ─────────────────────────────────────────────────────────────

    private static FeedPostResponse MapToFeedPost(Post p, Guid? userId, HashSet<Guid> followingSet) => new()
    {
        Id = p.Id,
        EmployerId = p.EmployerId,
        EmployerName = p.Employer.User.FirstName + " " + p.Employer.User.LastName,
        EmployerAvatarUrl = p.Employer.User.AvatarUrl,
        WorkshopId = p.WorkshopId,
        WorkshopTitle = p.Workshop.Title,
        Caption = p.Caption,
        LikeCount = p.LikeCount,
        CommentCount = p.CommentCount,
        ShareCount = p.ShareCount,
        ViewCount = p.ViewCount,
        EngagementScore = p.EngagementScore,
        IsLikedByMe = userId != null && p.Likes.Any(l => l.UserId == userId),
        IsFollowingEmployer = followingSet.Contains(p.Employer.UserId),
        Media = p.Media
            .Where(m => m.ConfirmedAt != null)
            .OrderBy(m => m.OrderIndex)
            .Select(m => new FeedMediaResponse
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

    // ─── Cursor encoding ─────────────────────────────────────────────────────

    private static string EncodeFeedCursor(double score, Guid id)
    {
        var raw = $"{score:R}|{id}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
    }

    private static (double score, Guid id) DecodeFeedCursor(string cursor)
    {
        var raw = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
        var parts = raw.Split('|');
        return (double.Parse(parts[0]), Guid.Parse(parts[1]));
    }
}
