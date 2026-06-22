namespace CommunityPlatform.Domain.Entities;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Type { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string Channel { get; set; } = "in_app"; // in_app | push | email
    public bool IsRead { get; set; } = false;
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}