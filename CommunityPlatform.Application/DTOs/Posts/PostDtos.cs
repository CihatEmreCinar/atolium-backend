namespace CommunityPlatform.Application.DTOs.Posts;

// ─── Request ─────────────────────────────────────────────────────────────────

public class CreatePostRequest
{
    public Guid WorkshopId { get; set; }
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
    public Guid EmployerId { get; set; }
    public string EmployerName { get; set; } = null!;
    public string? EmployerAvatarUrl { get; set; }
    public Guid WorkshopId { get; set; }
    public string WorkshopTitle { get; set; } = null!;
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
