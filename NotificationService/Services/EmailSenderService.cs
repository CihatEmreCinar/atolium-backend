using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using NotificationService.Models;

namespace NotificationService.Services;

public class EmailSenderService(IConfiguration config, ILogger<EmailSenderService> logger)
{
    public async Task SendAsync(EmailMessage message)
    {
        var smtp = config.GetSection("Smtp");

        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(
            smtp["FromName"] ?? "Atolium",
            smtp["FromAddress"] ?? "no-reply@atolium.com"));
        email.To.Add(MailboxAddress.Parse(message.To));
        email.Subject = message.Subject;

        email.Body = new BodyBuilder
        {
            HtmlBody = message.IsHtml ? message.Body : null,
            TextBody = message.IsHtml ? null : message.Body
        }.ToMessageBody();

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
