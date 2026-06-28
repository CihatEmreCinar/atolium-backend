namespace CommunityPlatform.Domain.Entities;

public class Tag
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;

    // URL-safe, unique: "yazılım-geliştirme"
    public string Slug { get; set; } = null!;

    // Denormalized — trigger günceller, trending query için index'li
    public int UsageCount { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
}

public class PostTag
{
    public Guid PostId { get; set; }
    public Guid TagId { get; set; }

    // Navigation
    public Post Post { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
