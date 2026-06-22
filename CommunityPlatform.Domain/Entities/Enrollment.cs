namespace CommunityPlatform.Domain.Entities;

public class Enrollment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkshopId { get; set; }
    public Guid UserId { get; set; }
    public Guid? PaymentId { get; set; }
    public string Status { get; set; } = "pending"; // pending | confirmed | cancelled | attended
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? AttendedAt { get; set; }
    public string TicketCode { get; set; } = Guid.NewGuid().ToString("N");

    // Navigation
    public Workshop Workshop { get; set; } = null!;
    public User User { get; set; } = null!;
    public Payment? Payment { get; set; }
}