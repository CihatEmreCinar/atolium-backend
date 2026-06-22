namespace CommunityPlatform.Application.DTOs.Notifications;

public class NotificationResponse
{
    public Guid Id { get; set; }
    public string Type { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string Channel { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}