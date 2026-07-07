namespace CommunityPlatform.Application.DTOs.SpaceListings;

public class UpdateSpaceListingRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? Capacity { get; set; }
    public decimal? HourlyPrice { get; set; }
    public List<string>? Amenities { get; set; }
    public bool? IsActive { get; set; }
}
