using CommunityPlatform.Domain.Enums;

namespace CommunityPlatform.Domain.Entities;

public class SpaceBooking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SpaceListingId { get; set; }
    public Guid EmployerProfileId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public SpaceBookingStatus Status { get; set; } = SpaceBookingStatus.Pending;
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public SpaceListing SpaceListing { get; set; } = null!;
    public EmployerProfile EmployerProfile { get; set; } = null!;
}
