namespace CommunityPlatform.Domain.Entities;

public class City
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    public string PlateCode { get; set; } = null!; // "01".."81"

    // Navigation
    public ICollection<District> Districts { get; set; } = [];
}
