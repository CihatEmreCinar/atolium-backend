using System.Net.Http.Json;
using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Domain.Enums;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CommunityPlatform.Infrastructure.BackgroundJobs;

/// <summary>
/// EngagementScoreJob ile birebir aynı iskelet: IServiceScopeFactory ile scope açar,
/// belirli aralıklarla çalışır, hata durumunda loglayıp devam eder.
///
/// SentAt IS NULL ve zamanı gelmiş EventReminder satırlarını bulur, kullanıcının
/// DeviceToken'larına Expo Push API üzerinden bildirim gönderir.
///
/// NOT (EngagementScoreJob'dan KASITLI fark): burada interval sabit "static readonly
/// TimeSpan" değil, appsettings'teki "Reminders:DispatchIntervalSeconds" değerinden
/// okunuyor (varsayılan 300 sn / 5 dk). Bunun sebebi görevin DOĞRULAMA adım 6'sının
/// test için interval'ı geçici olarak 30 saniyeye çekmeyi istemesi — hardcoded bir
/// sabitle bu mümkün olmazdı.
/// </summary>
public class ReminderDispatchJob(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<ReminderDispatchJob> logger) : BackgroundService
{
    private const string ExpoPushUrl = "https://exp.host/--/api/v2/push/send";
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ReminderDispatchJob başlatıldı.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ReminderDispatchJob hatası.");
            }

            await Task.Delay(GetInterval(), stoppingToken);
        }
    }

    private TimeSpan GetInterval()
    {
        var seconds = configuration.GetValue<int?>("Reminders:DispatchIntervalSeconds");
        return seconds.HasValue ? TimeSpan.FromSeconds(seconds.Value) : DefaultInterval;
    }

    private async Task RunAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;

        // SELECT * FROM "EventReminders" WHERE "SentAt" IS NULL
        //   AND NOW() >= "EventStartAt" - ("OffsetMinutes" * INTERVAL '1 minute')
        var dueReminders = await db.EventReminders
            .Where(r => r.SentAt == null && now >= r.EventStartAt.AddMinutes(-r.OffsetMinutes))
            .ToListAsync(ct);

        if (dueReminders.Count == 0) return;

        var userIds = dueReminders.Select(r => r.UserId).Distinct().ToList();
        var tokensByUser = await db.DeviceTokens
            .Where(t => userIds.Contains(t.UserId))
            .ToListAsync(ct);

        var client = httpClientFactory.CreateClient();

        foreach (var reminder in dueReminders)
        {
            var tokens = tokensByUser.Where(t => t.UserId == reminder.UserId).ToList();
            var (title, body) = BuildMessage(reminder);

            foreach (var token in tokens)
            {
                try
                {
                    var response = await client.PostAsJsonAsync(ExpoPushUrl, new
                    {
                        to = token.ExpoPushToken,
                        title,
                        body,
                        data = new { sourceType = reminder.SourceType.ToString(), sourceId = reminder.SourceId }
                    }, ct);

                    if (!response.IsSuccessStatusCode)
                    {
                        logger.LogWarning(
                            "Expo push başarısız. ReminderId={ReminderId} Status={Status}",
                            reminder.Id, response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    // Tek deneme yeterli — retry mantığı bilerek yok (over-engineering yapma).
                    logger.LogWarning(ex, "Expo push gönderilirken hata. ReminderId={ReminderId}", reminder.Id);
                }
            }

            // İstek başarılı/başarısız fark etmez, SentAt işaretlenir.
            reminder.SentAt = now;
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation("ReminderDispatchJob: {Count} hatırlatma işlendi.", dueReminders.Count);
    }

    private static (string Title, string Body) BuildMessage(EventReminder reminder)
    {
        var humanOffset = ToHumanReadable(reminder.OffsetMinutes);

        return reminder.SourceType switch
        {
            ReminderSourceType.Workshop => ("Atölye hatırlatması", $"Workshopun {humanOffset} başlıyor."),
            ReminderSourceType.SpaceBooking => ("Rezervasyon hatırlatması", $"Rezervasyonun {humanOffset} başlıyor."),
            _ => ("Hatırlatma", $"Etkinliğin {humanOffset} başlıyor.")
        };
    }

    private static string ToHumanReadable(int offsetMinutes) => offsetMinutes switch
    {
        120 => "2 saat sonra",
        1440 => "1 gün sonra",
        4320 => "3 gün sonra",
        _ => $"{offsetMinutes} dakika sonra"
    };
}
