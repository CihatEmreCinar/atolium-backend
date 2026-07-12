using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Hosting;
using MimeKit;
using NotificationService.Models;

namespace NotificationService.Services;

public class EmailSenderService(
    IConfiguration config,
    IHostEnvironment env,
    ILogger<EmailSenderService> logger)
{
    private readonly string _logoPath = Path.Combine(env.ContentRootPath, "Assets", "logo.png");

    public async Task SendAsync(EmailMessage message)
    {
        var smtp = config.GetSection("Smtp");

        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(
            smtp["FromName"] ?? "Atolium",
            smtp["FromAddress"] ?? "no-reply@atolium.com"));
        email.To.Add(MailboxAddress.Parse(message.To));
        email.Subject = message.Subject;

        var builder = new BodyBuilder();

        if (message.IsHtml)
        {
            // Logo dosyası varsa gövdeye "cid:logo" olarak göm.
            if (File.Exists(_logoPath))
            {
                var logo = builder.LinkedResources.Add(_logoPath);
                logo.ContentId = "logo";
                builder.HtmlBody = message.Body; // Body zaten <img src="cid:logo"> içeriyor
            }
            else
            {
                logger.LogWarning("Logo bulunamadı: {Path}, gövde logosuz gönderiliyor.", _logoPath);
                builder.HtmlBody = message.Body;
            }
        }
        else
        {
            builder.TextBody = message.Body;
        }

        email.Body = builder.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(
                smtp["Host"],
                int.Parse(smtp["Port"] ?? "587"),
                SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(smtp["Username"], smtp["Password"]);
            await client.SendAsync(email);
            await client.DisconnectAsync(true);

            logger.LogInformation("Email gönderildi → {To} | Konu: {Subject}", message.To, message.Subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Email gönderilemedi → {To}", message.To);
            throw; // consumer retry mekanizması devreye girer
        }
    }
}