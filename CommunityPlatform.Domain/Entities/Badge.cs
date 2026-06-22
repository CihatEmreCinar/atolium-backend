namespace CommunityPlatform.Domain.Entities;

public class Badge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string IconUrl { get; set; } = null!;
    public int XpReward { get; set; } = 0;
    public string TriggerType { get; set; } = null!;
    public int TriggerThreshold { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<UserBadge> UserBadges { get; set; } = [];
}