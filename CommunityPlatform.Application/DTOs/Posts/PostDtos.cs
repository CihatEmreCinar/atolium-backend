namespace CommunityPlatform.Application.DTOs.Posts;

// ─── Request ─────────────────────────────────────────────────────────────────

public class CreatePostRequest
{
    // Employer post'u için zorunlu, Cafe post'u için gönderilmez (null).
    public Guid? WorkshopId { get; set; }
    public string? Caption { get; set; }
    public List<string> TagSlugs { get; set; } = new();
}

public class UpdatePostRequest
{
    public string? Caption { get; set; }
    public List<string> TagSlugs { get; set; } = new();
}

public class ConfirmMediaRequest
{
    public Guid MediaId { get; set; }
    public int? WidthPx { get; set; }
    public int? HeightPx { get; set; }
}

// ─── Response ────────────────────────────────────────────────────────────────

public class PostResponse
{
    public Guid Id { get; set; }

    // AuthorType == Employer ise dolu, Cafe ise null
    public Guid? EmployerId { get; set; }
    public string? EmployerName { get; set; }
    public string? EmployerAvatarUrl { get; set; }

    // AuthorType == Cafe ise dolu, Employer ise null
    public Guid? CafeId { get; set; }
    public string? CafeName { get; set; }
    public string? CafeAvatarUrl { get; set; }

    // Employer post'u ise dolu, Cafe post'u ise null
    public Guid? WorkshopId { get; set; }
    public string? WorkshopTitle { get; set; }

    public string AuthorType { get; set; } = null!;
    public string Visibility { get; set; } = null!;

    public string? Caption { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public int ShareCount { get; set; }
    public int ViewCount { get; set; }
    public bool IsLikedByMe { get; set; }
    public bool IsFollowingEmployer { get; set; }
    public List<PostMediaResponse> Media { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public DateTime PublishedAt { get; set; }
}

public class PostMediaResponse
{
    public Guid Id { get; set; }
    public string MediaType { get; set; } = null!;
    public string Url { get; set; } = null!;
    public short OrderIndex { get; set; }
    public int? WidthPx { get; set; }
    public int? HeightPx { get; set; }
}

public class FeedResponse
{
    public List<PostResponse> Posts { get; set; } = new();

    /// <summary>
    /// Sonraki sayfa için cursor. NULL ise daha fazla post yok.
    /// Frontend bunu bir sonraki istekte ?cursor= parametresi olarak gönderir.
    /// </summary>
    public string? NextCursor { get; set; }

    public bool HasNextPage { get; set; }
}

public class UploadMediaResponse
{
    public Guid MediaId { get; set; }
    public string UploadUrl { get; set; } = null!;  // Local'de direkt POST endpoint'i
    public short OrderIndex { get; set; }
}

// PostDtos.cs içine eklenecek — mevcut namespace ve using'lerin altına

public class PostListResponse
{
    public List<PostResponse> Posts { get; set; } = new();
    public string? NextCursor { get; set; }
    public bool HasNextPage { get; set; }
}

public class UserSocialStats
{
    public int PostCount { get; set; }
    public int FollowerCount { get; set; }
    public int FollowingCount { get; set; }
    public bool IsFollowedByMe { get; set; }
}