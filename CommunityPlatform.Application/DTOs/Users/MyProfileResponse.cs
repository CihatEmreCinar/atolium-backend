namespace CommunityPlatform.Application.DTOs.Users;

public class MyProfileResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string? City { get; set; }
    public Guid? CityId { get; set; }
    public string? District { get; set; }
    public Guid? DistrictId { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public int XpPoints { get; set; }
    public int RankLevel { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public MyEmployeeProfileResponse? EmployeeProfile { get; set; }
    public MyEmployerProfileResponse? EmployerProfile { get; set; }
}

public class MyEmployeeProfileResponse
{
    public List<string> Interests { get; set; } = [];
    public List<string> Hobbies { get; set; } = [];
    public int TotalAttendedWorkshops { get; set; }
}

public class MyEmployerProfileResponse
{
    public string WorkshopTitle { get; set; } = null!;
    public List<string> Specialization { get; set; } = [];
    public List<Guid> CategoryIds { get; set; } = [];
    public List<string> CategoryNames { get; set; } = [];
    public int? YearsExperience { get; set; }
    public string? CoverImageUrl { get; set; }
    public decimal AvgRating { get; set; }
    public int TotalWorkshops { get; set; }
    public string EmployerRank { get; set; } = null!;
}