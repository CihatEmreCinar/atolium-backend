using CommunityPlatform.Domain.Enums;

namespace CommunityPlatform.Domain.Entities;

public class SpaceListing
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CafeProfileId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public int Capacity { get; set; }
    public decimal HourlyPrice { get; set; }
    public List<string> Amenities { get; set; } = [];
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public CafeProfile CafeProfile { get; set; } = null!;
    public ICollection<SpaceAvailability> SpaceAvailabilities { get; set; } = [];
    public ICollection<SpaceBooking> SpaceBookings { get; set; } = [];
}
