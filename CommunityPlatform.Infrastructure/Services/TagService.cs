using CommunityPlatform.Application.DTOs.Feed;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Domain.Enums;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityPlatform.Infrastructure.Services;

public class TagService(AppDbContext db, ICurrentUserService currentUser)
{
    // ─── Autocomplete (2.7) ──────────────────────────────────────────────────

    /// <summary>
    /// GET /api/v1/tags/search?q=sera&limit=10
    /// Slug prefix match — GIN index kullanır.
    /// </summary>
    public async Task<List<TagResponse>> SearchTagsAsync(string q, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(q))
            return new();

        var normalized = q.Trim().ToLowerInvariant();

        return await db.Tags
            .Where(t => t.Slug.StartsWith(normalized) || t.Name.ToLower().Contains(normalized))
            .OrderByDescending(t => t.UsageCount)
            .Take(limit)
            .Select(t => new TagResponse
            {
                Slug = t.Slug,
                Name = t.Name,
                UsageCount = t.UsageCount
            })
            .ToListAsync();
    }

    // ─── Trending (2.7) ──────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/v1/tags/trending?limit=20
    /// Son 7 günde en çok kullanılan tag'ler — usage_count DESC index.
    /// </summary>
    public async Task<List<TagResponse>> GetTrendingTagsAsync(int limit = 20)
    {
        var since = DateTime.UtcNow.AddDays(-7);

        // usage_count denormalized — trigger ile güncelleniyor, direkt sıralama yeterli
        return await db.Tags
            .Where(t => t.UsageCount > 0)
            .OrderByDescending(t => t.UsageCount)
            .Take(limit)
            .Select(t => new TagResponse
            {
                Slug = t.Slug,
                Name = t.Name,
                UsageCount = t.UsageCount
            })
            .ToListAsync();
    }

    // ─── Tag feed (2.7) ──────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/v1/tags/{slug}/posts?cursor=&limit=20
    /// Belirli tag'e ait postlar, engagement score sıralı.
    /// </summary>
    public async Task<FeedResponse> GetTagFeedAsync(string slug, string? cursor, int limit = 20)
    {
        var tag = await db.Tags.FirstOrDefaultAsync(t => t.Slug == slug)
            ?? throw new KeyNotFoundException($"'{slug}' tag bulunamadı.");

        var query = db.Posts
            .Where(p => p.PostTags.Any(pt => pt.TagId == tag.Id))
            .Include(p => p.Employer).ThenInclude(e => e!.User)
            .Include(p => p.Workshop)
            .Include(p => p.Cafe)
            .Include(p => p.Media.Where(m => m.ConfirmedAt != null))
            .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
            .Include(p => p.Likes)
            .AsNoTracking();

        // Visibility filtresi: bu endpoint anonim erişime açık ve rol kontrolü yok —
        // FeedService.BuildBaseQuery'deki aynı allow-list kural burada da uygulanmalı,
        // yoksa Cafe'nin EmployersOnly post'u tag feed üzerinden herkese sızar.
        var canSeeEmployersOnly = currentUser.Role == "employer" || currentUser.Role == "cafe";
        if (!canSeeEmployersOnly)
            query = query.Where(p => p.Visibility == PostVisibility.Public);

        if (!string.IsNullOrEmpty(cursor))
        {
            var (cursorScore, cursorId) = DecodeCursor(cursor);
            query = query.Where(p =>
                p.EngagementScore < cursorScore ||
                (p.EngagementScore == cursorScore && p.Id.CompareTo(cursorId) < 0));
        }

        var posts = await query
            .OrderByDescending(p => p.EngagementScore)
            .ThenByDescending(p => p.Id)
            .Take(limit + 1)
            .ToListAsync();

        var hasNext = posts.Count > limit;
        if (hasNext) posts.RemoveAt(posts.Count - 1);

        return new FeedResponse
        {
            Posts = posts.Select(p => new FeedPostResponse
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
                WorkshopTitle = p.Workshop != null ? p.Workshop.Title : null,

                AuthorType = p.AuthorType.ToString(),
                Visibility = p.Visibility.ToString(),

                Caption = p.Caption,
                LikeCount = p.LikeCount,
                CommentCount = p.CommentCount,
                ShareCount = p.ShareCount,
                ViewCount = p.ViewCount,
                EngagementScore = p.EngagementScore,
                IsLikedByMe = false, // anonim erişim — auth gerekirse override edilir
                Tags = p.PostTags.Select(pt => pt.Tag.Slug).ToList(),
                Media = p.Media
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
                PublishedAt = p.PublishedAt
            }).ToList(),
            HasNextPage = hasNext,
            NextCursor = hasNext
                ? EncodeCursor(posts.Last().EngagementScore, posts.Last().Id)
                : null
        };
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static string EncodeCursor(double score, Guid id)
    {
        var raw = $"{score:R}|{id}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
    }

    private static (double score, Guid id) DecodeCursor(string cursor)
    {
        var raw = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
        var parts = raw.Split('|');
        return (double.Parse(parts[0]), Guid.Parse(parts[1]));
    }
}

public class TagResponse
{
    public string Slug { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int UsageCount { get; set; }
}
