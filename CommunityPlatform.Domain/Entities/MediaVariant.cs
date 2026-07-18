using CommunityPlatform.Domain.Enums;

namespace CommunityPlatform.Domain.Entities;

public class MediaVariant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MediaId { get; set; }

    public int Width { get; set; }
    public int Height { get; set; }

    public string ObjectKey { get; set; } = null!;
    public long FileSize { get; set; }

    public Media Media { get; set; } = null!;
}