namespace CommunityPlatform.Domain.Entities;

public class UserBadge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid BadgeId { get; set; }
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public Badge Badge { get; set; } = null!;
}