using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Infrastructure.Persistence;

namespace CommunityPlatform.Infrastructure.Services;

public static class NotificationHelper
{
    public static async Task CreateAsync(
        AppDbContext db,
        Guid userId,
        string type,
        string title,
        string body)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
            Channel = "in_app",
            IsRead = false
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync();
    }
}