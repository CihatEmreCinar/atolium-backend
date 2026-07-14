namespace CommunityPlatform.Application.DTOs.Cafe;

public class CafeProfileResponse
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string? Bio { get; set; }
    public string? City { get; set; }
    public Guid? CityId { get; set; }
    public string? District { get; set; }
    public Guid? DistrictId { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? AvatarUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public List<Guid> CategoryIds { get; set; } = [];
    public List<string> CategoryNames { get; set; } = [];
}
