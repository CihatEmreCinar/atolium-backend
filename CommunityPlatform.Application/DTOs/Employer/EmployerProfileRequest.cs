namespace CommunityPlatform.Application.DTOs.Employer;

public class EmployerProfileRequest
{
    public string WorkshopTitle { get; set; } = null!;
    public List<string>? Specialization { get; set; }
    public List<Guid>? CategoryIds { get; set; }
    public int? YearsExperience { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
}