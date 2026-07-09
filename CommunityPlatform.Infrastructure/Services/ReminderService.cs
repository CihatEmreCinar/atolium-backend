using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Domain.Enums;
using CommunityPlatform.Infrastructure.Persistence;

namespace CommunityPlatform.Infrastructure.Services;

public class ReminderService(AppDbContext db) : IReminderService
{
    // 2 saat / 1 gün / 3 gün önce
    private static readonly int[] OffsetsMinutes = [120, 1440, 4320];

    public async Task CreateRemindersAsync(Guid userId, ReminderSourceType sourceType, Guid sourceId, DateTime eventStartAt)
    {
        foreach (var offset in OffsetsMinutes)
        {
            db.EventReminders.Add(new EventReminder
            {
                UserId = userId,
                SourceType = sourceType,
                SourceId = sourceId,
                EventStartAt = eventStartAt,
                OffsetMinutes = offset
            });
        }

        await db.SaveChangesAsync();
    }
}
