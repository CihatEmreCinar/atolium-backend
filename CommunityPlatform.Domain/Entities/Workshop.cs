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
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Status { get; set; } = "draft"; // draft | published | cancelled | completed
    public List<string> Tags { get; set; } = [];
    public decimal AvgRating { get; set; } = 0;
    public int ReviewCount { get; set; } = 0;
    public bool IsFeatured { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
    public uint Version { get; set; } // Postgres xmin sistem kologuna eşlenecek — concurrency token

    // Navigation
    public User Employer { get; set; } = null!;
    public ICollection<WorkshopCategory> WorkshopCategories { get; set; } = [];
    public ICollection<Enrollment> Enrollments { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
}