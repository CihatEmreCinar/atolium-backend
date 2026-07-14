namespace CommunityPlatform.Application.DTOs.Employer;

public class EmployerProfileResponse
{
    public Guid UserId { get; set; }
    public string WorkshopTitle { get; set; } = null!;
    public List<string> Specialization { get; set; } = [];
    public List<Guid> CategoryIds { get; set; } = [];
    public List<string> CategoryNames { get; set; } = [];
    public int? YearsExperience { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? City { get; set; }
    public Guid? CityId { get; set; }
    public string? District { get; set; }
    public Guid? DistrictId { get; set; }
    public decimal AvgRating { get; set; }
    public int TotalWorkshops { get; set; }
    public string EmployerRank { get; set; } = null!;
}

public class EmployerDashboardResponse
{
    public int ActiveWorkshops { get; set; }
    public int TotalWorkshops { get; set; }
    public int PendingEnrollments { get; set; }
    public int TotalEnrollments { get; set; }
    public decimal AvgRating { get; set; }
    public int ReviewCount { get; set; }
    public int XpPoints { get; set; }
    public int RankLevel { get; set; }
    public string EmployerRank { get; set; } = null!;
}