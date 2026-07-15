using System.Text.Json;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityPlatform.Infrastructure.Services;

public class NotificationService(
    AppDbContext db,
    IRabbitMqPublisher publisher,
    IPushNotificationSender pushSender) : INotificationService
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

        // 2. Gerçek OS push bildirimi gönder (Expo).
        await pushSender.SendAsync(userId, title, body, new { type = type.ToString(), metadata });

        // 3. Email isteniyorsa kullanıcının adresini al ve kuyruğa ekle
        if (sendEmail)
        {
            var user = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.Email != null)
            {
                await PublishGenericNotificationEmailAsync(user.Email, title, body);
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

        // 2. Toplu push
        await pushSender.SendManyAsync(idList, title, body, new { type = type.ToString(), metadata });

        // 3. Email isteniyorsa her kullanıcı için kuyruğa ekle
        if (sendEmail)
        {
            var users = await db.Users
                .AsNoTracking()
                .Where(u => idList.Contains(u.Id) && u.Email != null)
                .Select(u => new { u.Id, u.Email })
                .ToListAsync();

            foreach (var user in users)
            {
                await PublishGenericNotificationEmailAsync(user.Email!, title, body);
            }
        }
    }

    public async Task SendTicketEmailAsync(
        string toEmail,
        string displayName,
        string workshopTitle,
        DateTime workshopStartsAtUtc,
        string locationName,
        string ticketCode,
        byte[] qrCodePng)
    {
        await publisher.PublishAsync(EmailQueue, new
        {
            EventType = "TicketEvent",
            Payload = new
            {
                ToEmail = toEmail,
                DisplayName = displayName,
                WorkshopTitle = workshopTitle,
                // DateTimeOffset'e sıfır offset ile açıkça sarmalanıyor: Postgres'ten gelen
                // DateTime'ın Kind'i (Utc/Unspecified) ne olursa olsun, worker tarafında
                // DateTimeOffset'e deserialize edilirken yanlış yerel saat dilimi
                // varsayılmasını engeller.
                WorkshopStartsAtUtc = new DateTimeOffset(DateTime.SpecifyKind(workshopStartsAtUtc, DateTimeKind.Utc)),
                LocationName = locationName,
                TicketCode = ticketCode,
                QrCodePng = qrCodePng
            }
        });
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private Task PublishGenericNotificationEmailAsync(string toEmail, string title, string body) =>
        publisher.PublishAsync(EmailQueue, new
        {
            EventType = "GenericNotificationEvent",
            Payload = new { ToEmail = toEmail, Title = title, Body = body }
        });

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
}
