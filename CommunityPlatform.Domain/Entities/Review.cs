using CommunityPlatform.Domain.Enums;

namespace CommunityPlatform.Domain.Entities;

public class Review
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkshopId { get; set; }
    public Guid UserId { get; set; }
    public Guid? SpaceBookingId { get; set; }
    public int Rating { get; set; } // 1-5
    public string? Comment { get; set; }
    public RevieweeType RevieweeType { get; set; } = RevieweeType.Employer;
    public bool IsVisible { get; set; } = true;
    public string? EmployerReply { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Workshop Workshop { get; set; } = null!;
    public User User { get; set; } = null!;
    public SpaceBooking? SpaceBooking { get; set; }
}