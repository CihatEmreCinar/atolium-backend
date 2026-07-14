namespace CommunityPlatform.Domain.Entities;

public class EmployeeProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public List<string> Interests { get; set; } = [];
    public List<string> Hobbies { get; set; } = [];
    public Guid? PreferredCityId { get; set; }
    public Guid? PreferredDistrictId { get; set; }
    public string? CoverImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public City? PreferredCity { get; set; }
    public District? PreferredDistrict { get; set; }
}