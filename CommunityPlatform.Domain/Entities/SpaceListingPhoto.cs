namespace CommunityPlatform.Domain.Entities;

public class SpaceListingPhoto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SpaceListingId { get; set; }

    // Storage path (local: "uploads/spaces/{listingId}/{filename}")
    public string StorageKey { get; set; } = null!;

    // Serve edilecek URL — local'de relative ("/uploads/..."), ileride CDN absolute
    public string Url { get; set; } = null!;

    // Galeri sırası — ilk fotoğraf kart görselinde kullanılır
    public short OrderIndex { get; set; } = 0;
    public long? SizeBytes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation2
    public SpaceListing SpaceListing { get; set; } = null!;
}
