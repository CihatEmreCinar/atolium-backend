namespace CommunityPlatform.Application.DTOs.Employer;

public class EmployerPublicProfileResponse
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string WorkshopTitle { get; set; } = null!;
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
    public List<string> Specialization { get; set; } = [];
    public List<string> CategoryNames { get; set; } = [];
    public int? YearsExperience { get; set; }
    public decimal AvgRating { get; set; }
    public int TotalWorkshops { get; set; }
    public string EmployerRank { get; set; } = null!;
    public List<PublicWorkshopItem> Workshops { get; set; } = [];
}

public class PublicWorkshopItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public decimal Price { get; set; }
    public decimal AvgRating { get; set; }
    public DateTime StartAt { get; set; }
}