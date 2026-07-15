using System.Text.Json;
using CommunityPlatform.Application.DTOs.Notifications;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;

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

        // 2. Gerçek OS push bildirimi gönder (Expo). ÖNCEDEN BU ADIM YOKTU — bildirim
        // sadece DB'ye yazılıyordu, hiçbir zaman push edilmiyordu (event reminder'lar
        // hariç). "Uygulama içinde görünüyor ama telefonun bildirim panelinde hiç
        // çıkmıyor" şikayetinin kök nedeni buydu.
        await pushSender.SendAsync(userId, title, body, new { type = type.ToString(), metadata });

        // 3. Email isteniyorsa kullanıcının adresini al ve kuyruğa ekle
        if (sendEmail)
        {
            var user = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.Email != null)
            {
            await publisher.PublishAsync(EmailQueue, new
            {
                EventType = "GenericNotificationEvent",
                Payload = new { ToEmail = user.Email, Title = title, Body = body }
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

        // 2. Toplu push — bkz. NotifyAsync'teki not, aynı eksiklik burada da vardı.
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
              await publisher.PublishAsync(EmailQueue, new
              {
                  EventType = "GenericNotificationEvent",
                  Payload = new { ToEmail = user.Email, Title = title, Body = body }
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
  }