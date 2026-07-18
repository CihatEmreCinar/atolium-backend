using CommunityPlatform.Domain.Enums;

namespace CommunityPlatform.Domain.Entities;

public class Media
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }

    public MediaPreset Preset { get; set; }
    public MediaStatus Status { get; set; } = MediaStatus.Pending;

    public string Bucket { get; set; } = null!;

    // Worker tamamlanana kadar orijinal WEBP'in geçici konumu.
    public string TempObjectKey { get; set; } = null!;

    // Final (worker tarafından işlenmiş) objenin key'i — Pending durumda null.
    public string? ObjectKey { get; set; }

    public string MimeType { get; set; } = "image/webp";
    public int? Width { get; set; }
    public int? Height { get; set; }
    public long? FileSize { get; set; }

    // SHA-256 hex — duplicate tespiti için (worker doldurur).
    public string? Checksum { get; set; }

    public string? FailureReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    public ICollection<MediaVariant> Variants { get; set; } = new List<MediaVariant>();
}