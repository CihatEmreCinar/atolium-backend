namespace CommunityPlatform.Application.DTOs.Workshops;

public class WorkshopResponse
{
    public Guid Id { get; set; }
    public Guid EmployerId { get; set; }
    public string EmployerName { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public decimal Price { get; set; }
    public int Capacity { get; set; }
    public int EnrolledCount { get; set; }
    public string LocationType { get; set; } = null!;
    public string? LocationDetail { get; set; }
    public string? VenueName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public Guid? CityId { get; set; }
    public string? District { get; set; }
    public Guid? DistrictId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Status { get; set; } = null!;
    public List<string> Tags { get; set; } = [];
    public List<Guid> CategoryIds { get; set; } = [];
    public List<string> CategoryNames { get; set; } = [];
    public decimal AvgRating { get; set; }
    public int ReviewCount { get; set; }
    public DateTime CreatedAt { get; set; }

    // Sadece GET /workshops/nearby ile latitude/longitude verildiğinde dolar.
    public double? DistanceKm { get; set; }
}