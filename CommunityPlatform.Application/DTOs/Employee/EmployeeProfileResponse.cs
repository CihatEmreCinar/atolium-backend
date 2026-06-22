namespace CommunityPlatform.Application.DTOs.Employee;

public class EmployeeProfileResponse
{
    public Guid UserId { get; set; }
    public List<string> Interests { get; set; } = [];
    public List<string> Hobbies { get; set; } = [];
    public int TotalAttendedWorkshops { get; set; }
    public int XpPoints { get; set; }
    public int RankLevel { get; set; }
}