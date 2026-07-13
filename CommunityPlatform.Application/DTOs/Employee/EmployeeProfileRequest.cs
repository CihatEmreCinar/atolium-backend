namespace CommunityPlatform.Application.DTOs.Employee;

public class EmployeeProfileRequest
{
    public List<string>? Interests { get; set; }
    public List<string>? Hobbies { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? City { get; set; }
}
