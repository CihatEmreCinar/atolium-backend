namespace NotificationService.Email.Contracts;

using NotificationService.Email.Models;

/// <summary>
/// Sağlayıcıdan bağımsız e-posta gönderim soyutlaması.
/// SMTP, MailKit, SendGrid, Azure Communication Services gibi farklı
/// sağlayıcılar bu arayüz üzerinden kolayca eklenebilir. Provider HTML üretmez.
/// </summary>
public interface IEmailProvider
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
