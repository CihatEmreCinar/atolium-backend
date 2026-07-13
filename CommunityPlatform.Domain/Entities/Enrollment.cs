namespace CommunityPlatform.Domain.Entities;

using CommunityPlatform.Domain.Enums;

public class Enrollment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkshopId { get; set; }
    public Guid UserId { get; set; }
    public Guid? PaymentId { get; set; }
    public string Status { get; set; } = "pending"; // pending | confirmed | cancelled
    public AttendanceStatus AttendanceStatus { get; set; } = AttendanceStatus.Pending;
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? AttendedAt { get; set; }

    // Navigation
    public Workshop Workshop { get; set; } = null!;
    public User User { get; set; } = null!;
    public Payment? Payment { get; set; }
    public ICollection<WorkshopTicket> Tickets { get; set; } = [];
}