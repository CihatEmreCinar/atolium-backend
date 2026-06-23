using System.Text.Json;
using CommunityPlatform.Application.DTOs.Notifications;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityPlatform.Infrastructure.Services;

public class NotificationService(
    AppDbContext db,
    IRabbitMqPublisher publisher) : INotificationService
{
    private const string EmailQueue = "notification.email";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task NotifyAsync(
        Guid userId,
        NotificationType type,
        string title,
        string body,
        object? metadata = null,
        bool sendEmail = false)
    {
        // 1. In-app bildirimi DB'ye yaz
        db.Notifications.Add(Build(userId, type, title, body, metadata));
        await db.SaveChangesAsync();

        // 2. Email isteniyorsa kullanıcının adresini al ve kuyruğa ekle
        if (sendEmail)
        {
            var user = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.Email != null)
            {
                await publisher.PublishAsync(EmailQueue, new EmailMessage
                {
                    To = user.Email,
                    Subject = title,
                    Body = BuildEmailBody(title, body),
                    IsHtml = true
                });
            }
        }
    }

    public async Task NotifyManyAsync(
        IEnumerable<Guid> userIds,
        NotificationType type,
        string title,
        string body,
        object? metadata = null,
        bool sendEmail = false)
    {
        var idList = userIds.ToList();
        var metaJson = metadata != null
            ? JsonSerializer.Serialize(metadata, JsonOptions)
            : null;

        // 1. Toplu in-app kayıt
        db.Notifications.AddRange(idList.Select(uid => new Notification
        {
            UserId = uid,
            Type = type,
            Title = title,
            Body = body,
            Channel = "in_app",
            Metadata = metaJson
        }));
        await db.SaveChangesAsync();

        // 2. Email isteniyorsa her kullanıcı için kuyruğa ekle
        if (sendEmail)
        {
            var users = await db.Users
                .AsNoTracking()
                .Where(u => idList.Contains(u.Id) && u.Email != null)
                .Select(u => new { u.Id, u.Email })
                .ToListAsync();

            foreach (var user in users)
            {
                await publisher.PublishAsync(EmailQueue, new EmailMessage
                {
                    To = user.Email!,
                    Subject = title,
                    Body = BuildEmailBody(title, body),
                    IsHtml = true
                });
            }
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Notification Build(
        Guid userId,
        NotificationType type,
        string title,
        string body,
        object? metadata) => new()
    {
        UserId = userId,
        Type = type,
        Title = title,
        Body = body,
        Channel = "in_app",
        Metadata = metadata != null
            ? JsonSerializer.Serialize(metadata, JsonOptions)
            : null
    };

    private static string BuildEmailBody(string title, string body) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:sans-serif;max-width:600px;margin:0 auto;padding:24px">
          <h2 style="color:#1a1a1a">{title}</h2>
          <p style="color:#444;line-height:1.6">{body}</p>
          <hr style="border:none;border-top:1px solid #eee;margin:24px 0"/>
          <p style="color:#999;font-size:12px">Atolium · Bu e-posta otomatik olarak gönderilmiştir.</p>
        </body>
        </html>
        """;
}
