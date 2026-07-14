namespace CommunityPlatform.Application.DTOs.Locations;

public class CityResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string PlateCode { get; set; } = null!;
}

public class DistrictResponse
{
    public Guid Id { get; set; }
    public Guid CityId { get; set; }
    public string Name { get; set; } = null!;
}
