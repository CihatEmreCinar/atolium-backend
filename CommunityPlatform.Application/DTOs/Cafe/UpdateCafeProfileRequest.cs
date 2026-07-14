namespace CommunityPlatform.Application.DTOs.Cafe;

public class UpdateCafeProfileRequest
{
    public string? Name { get; set; }
    public string? Bio { get; set; }
    public Guid? CityId { get; set; }
    public Guid? DistrictId { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public List<Guid>? CategoryIds { get; set; }
}
