namespace CommunityPlatform.Domain.Entities;

public class PostMedia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PostId { get; set; }

    public PostMediaType MediaType { get; set; }

    // Storage path (local: "uploads/posts/{postId}/{filename}")
    public string StorageKey { get; set; } = null!;

    // Serve edilecek URL — local'de relative, ileride CDN absolute
    public string CdnUrl { get; set; } = null!;

    // Carousel sırası — client side reorder yok, sunucu belirler
    public short OrderIndex { get; set; } = 0;

    public int? WidthPx { get; set; }
    public int? HeightPx { get; set; }
    public long? SizeBytes { get; set; }

    // NULL → upload henüz tamamlanmadı; feed query'de WHERE ConfirmedAt IS NOT NULL
    public DateTime? ConfirmedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Post Post { get; set; } = null!;
}

public enum PostMediaType
{
    Image,
    Video
}
