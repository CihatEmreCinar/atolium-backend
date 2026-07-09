using CommunityPlatform.Domain.Enums;

namespace CommunityPlatform.Application.Interfaces;

public interface IReminderService
{
    /// <summary>
    /// Bir event (Workshop / SpaceBooking) için kullanıcıya 3 hatırlatma satırı oluşturur:
    /// 120 dk (2 saat), 1440 dk (1 gün), 4320 dk (3 gün) önce.
    /// </summary>
    Task CreateRemindersAsync(Guid userId, ReminderSourceType sourceType, Guid sourceId, DateTime eventStartAt);
}
