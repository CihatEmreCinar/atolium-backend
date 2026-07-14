using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Domain.Enums;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CommunityPlatform.Infrastructure.BackgroundJobs;

public class ReminderDispatchJob(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<ReminderDispatchJob> logger) : BackgroundService
{
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
        var pushSender = scope.ServiceProvider.GetRequiredService<IPushNotificationSender>();

        var now = DateTime.UtcNow;

        var dueReminders = await db.EventReminders
            .Where(r => r.SentAt == null && now >= r.EventStartAt.AddMinutes(-r.OffsetMinutes))
            .ToListAsync(ct);

        if (dueReminders.Count == 0) return;

        foreach (var reminder in dueReminders)
        {
            var (title, body) = BuildMessage(reminder);

            await pushSender.SendAsync(
                reminder.UserId,
                title,
                body,
                new { sourceType = reminder.SourceType.ToString(), sourceId = reminder.SourceId },
                ct);

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