namespace CommunityPlatform.Application.DTOs.Notifications;

public class NotificationResponse
{
    public Guid Id { get; set; }

    /// <summary>
    /// NotificationType enum'un string karşılığı.
    /// Frontend bu değere göre ikon ve renk belirler.
    /// Örnek: "ApplicationApproved", "WorkshopReminder"
    /// </summary>
    public string Type { get; set; } = null!;

    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string Channel { get; set; } = null!;

    /// <summary>
    /// JSON string. Frontend parse ederek navigasyon hedefini belirler.
    /// Örnek: { "workshopId": "abc-123", "route": "workshop/detail" }
    /// </summary>
    public string? Metadata { get; set; }

    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
