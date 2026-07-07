namespace CommunityPlatform.Application.DTOs.SpaceListings;

public class CreateSpaceListingRequest
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public int Capacity { get; set; }
    public decimal HourlyPrice { get; set; }
    public List<string>? Amenities { get; set; }
}
