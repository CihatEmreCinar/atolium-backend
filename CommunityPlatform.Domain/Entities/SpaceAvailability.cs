namespace CommunityPlatform.Domain.Entities;

public class SpaceAvailability
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SpaceListingId { get; set; }
    public int DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public SpaceListing SpaceListing { get; set; } = null!;
}
