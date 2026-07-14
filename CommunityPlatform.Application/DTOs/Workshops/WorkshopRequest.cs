namespace CommunityPlatform.Application.DTOs.Workshops;

public class WorkshopRequest
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public decimal Price { get; set; }
    public int Capacity { get; set; }
    public string LocationType { get; set; } = null!; // online | in-person
    public string? LocationDetail { get; set; }

    // Fiziksel etkinlik konumu — sadece LocationType='in-person' iken anlamlı
    public string? VenueName { get; set; }
    public string? Address { get; set; }
    public Guid? CityId { get; set; }
    public Guid? DistrictId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public List<Guid>? CategoryIds { get; set; }
    public List<string>? Tags { get; set; }
}