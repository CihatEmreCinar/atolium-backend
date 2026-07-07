using CommunityPlatform.Domain.Enums;

namespace CommunityPlatform.Domain.Entities;

public class CafeProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string? Bio { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? AvatarUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ICollection<SpaceListing> SpaceListings { get; set; } = [];
    public ICollection<CafeProfileCategory> CafeProfileCategories { get; set; } = [];
}
