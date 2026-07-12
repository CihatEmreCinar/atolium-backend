namespace CommunityPlatform.Application.DTOs.SpaceListings;

public class SpaceListingResponse
{
    public Guid Id { get; set; }
    public Guid CafeProfileId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public int Capacity { get; set; }
    public decimal HourlyPrice { get; set; }
    public List<string> Amenities { get; set; } = [];
    public List<string> PhotoUrls { get; set; } = [];
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CafeName { get; set; }
    public string? CafeCity { get; set; }
    public string? CafeAvatarUrl { get; set; }
}
