namespace CommunityPlatform.Domain.Entities;

public class District
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CityId { get; set; }
    public string Name { get; set; } = null!;

    // Navigation
    public City City { get; set; } = null!;
}
