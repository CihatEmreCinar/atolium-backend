using CommunityPlatform.Domain.Enums;

namespace CommunityPlatform.Domain.Entities;

public class CafeProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string? Bio { get; set; }
    public Guid? CityId { get; set; }
    public Guid? DistrictId { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? AvatarUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public decimal AvgRating { get; set; } = 0;
    public int ReviewCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public City? City { get; set; }
    public District? District { get; set; }
    public ICollection<SpaceListing> SpaceListings { get; set; } = [];
    public ICollection<CafeProfileCategory> CafeProfileCategories { get; set; } = [];
}
