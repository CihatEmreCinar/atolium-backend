namespace CommunityPlatform.Domain.Entities;

public class Workshop
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployerId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public decimal Price { get; set; }
    public int Capacity { get; set; }
    public int EnrolledCount { get; set; } = 0;
    public string LocationType { get; set; } = null!; // online | in-person
    public string? LocationDetail { get; set; }

    // Fiziksel etkinlik konumu — Employer'ın kendi City'sinden bağımsız, workshop nerede
    // yapılıyorsa o. Sadece LocationType='in-person' iken anlamlı.
    public string? VenueName { get; set; }
    public string? Address { get; set; }
    public Guid? CityId { get; set; }
    public Guid? DistrictId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Status { get; set; } = "draft"; // draft | published | cancelled | completed
    public List<string> Tags { get; set; } = [];
    public decimal AvgRating { get; set; } = 0;
    public int ReviewCount { get; set; } = 0;
    public bool IsFeatured { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    // Navigation
    public User Employer { get; set; } = null!;
    public City? City { get; set; }
    public District? District { get; set; }
    public ICollection<WorkshopCategory> WorkshopCategories { get; set; } = [];
    public ICollection<Enrollment> Enrollments { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
}