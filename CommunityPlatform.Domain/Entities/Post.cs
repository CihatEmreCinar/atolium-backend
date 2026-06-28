namespace CommunityPlatform.Domain.Entities;

public class Post
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Zorunlu FK'lar
    public Guid EmployerId { get; set; }
    public Guid WorkshopId { get; set; }

    public string? Caption { get; set; }

    // Denormalized counter'lar — trigger + background job günceller
    public int LikeCount { get; set; } = 0;
    public int CommentCount { get; set; } = 0;
    public int ShareCount { get; set; } = 0;
    public int ViewCount { get; set; } = 0;

    // Feed ranking — Hangfire job her 5 dk'da bir günceller
    public double EngagementScore { get; set; } = 0.0;

    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public EmployerProfile Employer { get; set; } = null!;
    public Workshop Workshop { get; set; } = null!;
    public ICollection<PostMedia> Media { get; set; } = new List<PostMedia>();
    public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
    public ICollection<PostLike> Likes { get; set; } = new List<PostLike>();
    public ICollection<PostComment> Comments { get; set; } = new List<PostComment>();
    public ICollection<PostShare> Shares { get; set; } = new List<PostShare>();
}
