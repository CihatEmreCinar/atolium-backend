using CommunityPlatform.Domain.Enums;

namespace CommunityPlatform.Domain.Entities;

public class EventReminder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public ReminderSourceType SourceType { get; set; }
    public Guid SourceId { get; set; }
    public DateTime EventStartAt { get; set; }
    public int OffsetMinutes { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
