namespace CommunityPlatform.Application.DTOs.Feed;

public class FeedRequest
{
    /// <summary>Cursor-based pagination token — ilk istekte boş bırakılır</summary>
    public string? Cursor { get; set; }

    private int _limit = 20;
    public int Limit
    {
        get => _limit;
        set => _limit = value is < 1 or > 100 ? 20 : value;
    }

    /// <summary>Tag slug listesi — filtrele. Boşsa tümü gelir.</summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>Belirli bir workshop'un postlarını filtrele</summary>
    public Guid? WorkshopId { get; set; }
}

public class FeedResponse
{
    public List<FeedPostResponse> Posts { get; set; } = new();
    public string? NextCursor { get; set; }
    public bool HasNextPage { get; set; }
}

public class FeedPostResponse
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
    public double EngagementScore { get; set; }
    public bool IsLikedByMe { get; set; }
    public bool IsFollowingEmployer { get; set; }
    public List<FeedMediaResponse> Media { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public DateTime PublishedAt { get; set; }
}

public class FeedMediaResponse
{
    public Guid Id { get; set; }
    public string MediaType { get; set; } = null!;
    public string Url { get; set; } = null!;
    public short OrderIndex { get; set; }
    public int? WidthPx { get; set; }
    public int? HeightPx { get; set; }
}
