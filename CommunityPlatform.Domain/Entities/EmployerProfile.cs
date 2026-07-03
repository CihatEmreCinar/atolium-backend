namespace CommunityPlatform.Domain.Entities;

public class EmployerProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string WorkshopTitle { get; set; } = null!;
    public List<string> Specialization { get; set; } = [];
    public int? YearsExperience { get; set; }
    public string? CoverImageUrl { get; set; }
    public decimal AvgRating { get; set; } = 0;
    public int TotalWorkshops { get; set; } = 0;
    public string EmployerRank { get; set; } = "Yeni";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<EmployerProfileCategory> EmployerProfileCategories { get; set; } = [];
    public ICollection<Workshop> Workshops { get; set; } = [];
}