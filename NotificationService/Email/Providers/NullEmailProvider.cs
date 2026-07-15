namespace NotificationService.Email.Providers;

using Microsoft.Extensions.Logging;
using NotificationService.Email.Contracts;
using NotificationService.Email.Models;

/// <summary>
/// Geliştirme/staging ortamlarında gerçek SMTP kullanmadan pipeline'ı uçtan
/// uca test etmek için kullanılan sağlayıcı (Email:UseConsoleProvider = true).
/// </summary>
public sealed class NullEmailProvider : IEmailProvider
{
    private readonly ILogger<NullEmailProvider> _logger;

    public NullEmailProvider(ILogger<NullEmailProvider> logger) => _logger = logger;

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[NullEmailProvider] Gönderim simüle edildi -> To: {To}, Subject: {Subject}, BodyLength: {Length}, InlineImages: {InlineImageCount}",
            message.ToEmail, message.Subject, message.HtmlBody.Length, message.InlineImages?.Count ?? 0);
        return Task.CompletedTask;
    }
}
