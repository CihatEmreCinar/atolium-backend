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
    <html lang="tr">
    <head>
      <meta charset="utf-8">
      <meta name="viewport" content="width=device-width, initial-scale=1.0">
      <title>{title}</title>
    </head>
    <body style="margin:0;padding:0;background-color:#f4f6f5;font-family:-apple-system,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:#f4f6f5;padding:32px 16px;">
        <tr>
          <td align="center">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;background-color:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 1px 3px rgba(0,0,0,0.08);">
              <tr>
                <td style="padding:32px 32px 20px 32px;text-align:center;border-bottom:4px solid #068D84;">
                  <img src="cid:logo" width="64" height="64" alt="Atolium" style="display:block;margin:0 auto 10px auto;border-radius:16px;">
                  <span style="color:#068D84;font-size:17px;font-weight:700;letter-spacing:0.3px;">ATOLIUM</span>
                </td>
              </tr>
              <tr>
                <td style="padding:36px 32px 8px 32px;">
                  <h1 style="margin:0 0 16px 0;color:#111827;font-size:20px;font-weight:700;">{title}</h1>
                  <p style="margin:0;color:#4b5563;font-size:15px;line-height:1.65;">{body}</p>
                </td>
              </tr>
              <tr>
                <td style="padding:28px 32px 32px 32px;">
                  <hr style="border:none;border-top:1px solid #eef0ef;margin:0 0 20px 0;">
                  <p style="margin:0;color:#9ca3af;font-size:12px;line-height:1.5;">
                    Bu e-posta Atolium tarafından otomatik olarak gönderilmiştir.<br>
                    Sorularınız için bize ulaşabilirsiniz.
                  </p>
                </td>
              </tr>
            </table>
          </td>
        </tr>
      </table>
    </body>
    </html>
    """;
}
