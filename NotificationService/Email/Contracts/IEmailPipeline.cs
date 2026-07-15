namespace NotificationService.Email.Contracts;

using NotificationService.Email.Events;

/// <summary>
/// Consumer tarafından tetiklenen tek giriş noktası.
/// Resolve -> Build -> Render -> Send adımlarını sırasıyla yürütür.
/// </summary>
public interface IEmailPipeline
{
    Task ExecuteAsync(EmailEventBase emailEvent, CancellationToken cancellationToken = default);
}
