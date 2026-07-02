namespace CommunityPlatform.Application.DTOs.Enrollments;

public class EmployerEnrollmentResponse
{
    public Guid Id { get; set; }
    public string WorkshopTitle { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime AppliedAt { get; set; }
}