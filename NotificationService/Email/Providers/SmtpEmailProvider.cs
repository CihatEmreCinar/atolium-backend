namespace NotificationService.Email.Providers;

using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MimeKit;
using NotificationService.Email.Contracts;
using NotificationService.Email.Models;

/// <summary>
/// MailKit tabanlı SMTP implementasyonu (eski EmailSenderService.cs'in yerini alır).
/// Farklı bir sağlayıcıya (SendGrid, Azure Communication Services, vb.) geçmek için
/// yalnızca IEmailProvider'ın yeni bir implementasyonu yazılıp DI'da bu sınıfın
/// yerine kaydedilir; Pipeline, Consumer veya Builder'lara dokunulmaz.
///
/// Assets/logo.png, tüm e-postalarda otomatik olarak cid:logo olarak gömülür —
/// tıpkı eski EmailSenderService'teki davranış gibi (Templates/Base.html ve
/// Ticket.html zaten <img src="cid:logo"> referansı kullanıyor).
/// </summary>
public sealed class SmtpEmailProvider : IEmailProvider
{
    private readonly SmtpOptions _options;
    private readonly string _logoPath;

    public SmtpEmailProvider(IOptions<SmtpOptions> options, IHostEnvironment environment)
    {
        _options = options.Value;
        _logoPath = Path.Combine(environment.ContentRootPath, "Assets", "logo.png");
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        mime.To.Add(message.ToName is null
            ? MailboxAddress.Parse(message.ToEmail)
            : new MailboxAddress(message.ToName, message.ToEmail));
        mime.Subject = message.Subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = message.HtmlBody };

        if (File.Exists(_logoPath))
        {
            var logo = bodyBuilder.LinkedResources.Add(_logoPath);
            logo.ContentId = "logo";
        }

        if (message.InlineImages is { Count: > 0 })
        {
            foreach (var inline in message.InlineImages)
            {
                var resource = bodyBuilder.LinkedResources.Add(inline.ContentId, inline.Content, ContentType.Parse(inline.MediaType));
                resource.ContentId = inline.ContentId;
            }
        }

        if (message.Attachments is { Count: > 0 })
        {
            foreach (var attachment in message.Attachments)
            {
                bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.MediaType));
            }
        }

        mime.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            var secureOptions = _options.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
            await client.ConnectAsync(_options.Host, _options.Port, secureOptions, cancellationToken);

            if (!string.IsNullOrEmpty(_options.Username))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
            }

            await client.SendAsync(mime, cancellationToken);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true, cancellationToken);
            }
        }
    }
}
