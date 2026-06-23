using CommunityPlatform.Domain.Entities;

namespace CommunityPlatform.Application.Interfaces;

public interface INotificationService
{
    /// <summary>
    /// Tek kullanıcıya bildirim gönderir.
    /// sendEmail=true ise RabbitMQ üzerinden email kuyruğuna da ekler.
    /// </summary>
    Task NotifyAsync(
        Guid userId,
        NotificationType type,
        string title,
        string body,
        object? metadata = null,
        bool sendEmail = false);

    /// <summary>
    /// Birden fazla kullanıcıya toplu bildirim gönderir.
    /// sendEmail=true ise her kullanıcı için ayrı email kuyruğa eklenir.
    /// </summary>
    Task NotifyManyAsync(
        IEnumerable<Guid> userIds,
        NotificationType type,
        string title,
        string body,
        object? metadata = null,
        bool sendEmail = false);
}
