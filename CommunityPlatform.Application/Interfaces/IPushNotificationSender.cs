namespace CommunityPlatform.Application.Interfaces;

public interface IPushNotificationSender
{
    Task SendAsync(Guid userId, string title, string body, object? data = null, CancellationToken ct = default);

    Task SendManyAsync(IEnumerable<Guid> userIds, string title, string body, object? data = null, CancellationToken ct = default);
}