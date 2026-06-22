namespace CommunityPlatform.Application.DTOs.Enrollments;

public class EnrollmentResponse
{
    public Guid Id { get; set; }
    public Guid WorkshopId { get; set; }
    public string WorkshopTitle { get; set; } = null!;
    public DateTime WorkshopStartAt { get; set; }
    public string Status { get; set; } = null!;
    public string TicketCode { get; set; } = null!;
    public DateTime EnrolledAt { get; set; }
    public DateTime? AttendedAt { get; set; }
}