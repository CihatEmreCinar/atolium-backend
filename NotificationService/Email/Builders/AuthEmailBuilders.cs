namespace NotificationService.Email.Builders;

using Microsoft.Extensions.Options;
using NotificationService.Email.Events;
using NotificationService.Email.Models;
using NotificationService.Infrastructure;

public sealed class WelcomeEmailBuilder(IOptions<EmailBrandOptions> brand) : EmailBuilderBase<WelcomeEvent>(brand)
{
    protected override EmailTemplateModel BuildModel(WelcomeEvent e) => EmailTemplateModel.Create(
        hero: null,
        title: $"Atolium'a hoş geldin, {e.DisplayName}!",
        content: "Aramıza katıldığın için mutluyuz. Şehrindeki atölyeleri keşfetmeye, yeni beceriler öğrenmeye ve yaratıcı bir topluluğun parçası olmaya hazır mısın?",
        cta: new EmailButtonModel { PrimaryText = "Atölyeleri Keşfet", PrimaryUrl = $"{Footer().Website}/discover" },
        infoBox: null,
        footer: Footer());
}

public sealed class VerifyEmailBuilder(IOptions<EmailBrandOptions> brand) : EmailBuilderBase<VerifyEmailEvent>(brand)
{
    protected override EmailTemplateModel BuildModel(VerifyEmailEvent e) => EmailTemplateModel.Create(
        hero: null,
        title: "E-posta adresini doğrula",
        content: $"Merhaba {e.DisplayName}, hesabını aktifleştirmek için uygulamaya şu doğrulama kodunu gir:<br><br><strong style=\"font-size:28px;letter-spacing:6px;\">{e.VerificationCode}</strong><br><br>Bu kod {FormatExpiry(e.OtpExpiresIn)} içinde geçerliliğini yitirecek. Web'den devam etmek istersen aşağıdaki bağlantıyı kullanabilirsin; bağlantı {FormatExpiry(e.ExpiresIn)} geçerlidir.",
        cta: new EmailButtonModel { PrimaryText = "E-postamı Doğrula", PrimaryUrl = e.VerificationUrl },
        infoBox: new InformationBoxModel
        {
            Title = "Bu isteği sen yapmadıysan",
            Message = "Bu e-postayı görmezden gelebilirsin, hesabında herhangi bir değişiklik yapılmayacaktır.",
            IconUrl = DefaultInfoIconUrl,
        },
        footer: Footer());

    private static string FormatExpiry(TimeSpan span) =>
        span.TotalHours >= 1 ? $"{span.TotalHours:0} saat" : $"{span.TotalMinutes:0} dakika";
}

public sealed class MagicLinkEmailBuilder(IOptions<EmailBrandOptions> brand) : EmailBuilderBase<MagicLinkEvent>(brand)
{
    protected override EmailTemplateModel BuildModel(MagicLinkEvent e) => EmailTemplateModel.Create(
        hero: null,
        title: "Giriş bağlantın hazır",
        content: $"Merhaba {e.DisplayName}, aşağıdaki butona tıklayarak şifresiz giriş yapabilirsin. Bağlantı {(int)e.ExpiresIn.TotalMinutes} dakika içinde geçersiz olacak.",
        cta: new EmailButtonModel { PrimaryText = "Giriş Yap", PrimaryUrl = e.MagicLinkUrl },
        infoBox: null,
        footer: Footer());
}

public sealed class PasswordResetEmailBuilder(IOptions<EmailBrandOptions> brand) : EmailBuilderBase<PasswordResetEvent>(brand)
{
    protected override EmailTemplateModel BuildModel(PasswordResetEvent e) => EmailTemplateModel.Create(
        hero: null,
        title: "Şifreni sıfırla",
        content: $"Merhaba {e.DisplayName}, şifreni sıfırlamak için aşağıdaki butona tıkla. Bağlantı {(int)e.ExpiresIn.TotalMinutes} dakika içinde geçerliliğini yitirecek.",
        cta: new EmailButtonModel { PrimaryText = "Şifremi Sıfırla", PrimaryUrl = e.ResetUrl },
        infoBox: new InformationBoxModel
        {
            Title = "Bu isteği sen yapmadıysan",
            Message = "Hesabın güvende. Bu e-postayı görmezden gelip mevcut şifreni kullanmaya devam edebilirsin.",
            IconUrl = DefaultInfoIconUrl,
        },
        footer: Footer());
}

public sealed class PasswordChangedEmailBuilder(IOptions<EmailBrandOptions> brand) : EmailBuilderBase<PasswordChangedEvent>(brand)
{
    protected override EmailTemplateModel BuildModel(PasswordChangedEvent e) => EmailTemplateModel.Create(
        hero: null,
        title: "Şifren değiştirildi",
        content: $"Merhaba {e.DisplayName}, hesabının şifresi {e.ChangedAtUtc:dd MMMM yyyy HH:mm} tarihinde değiştirildi{(e.IpAddress is null ? "." : $" ({e.IpAddress} adresinden).")}",
        cta: null,
        infoBox: new InformationBoxModel
        {
            Title = "Bu işlemi sen yapmadıysan",
            Message = "Lütfen hemen destek ekibimizle iletişime geç.",
            IconUrl = DefaultInfoIconUrl,
        },
        footer: Footer());
}

public sealed class SecurityAlertEmailBuilder(IOptions<EmailBrandOptions> brand) : EmailBuilderBase<SecurityAlertEvent>(brand)
{
    protected override EmailTemplateModel BuildModel(SecurityAlertEvent e) => EmailTemplateModel.Create(
        hero: null,
        title: "Güvenlik uyarısı",
        content: $"Merhaba {e.DisplayName}, hesabınla ilgili bir güvenlik olayı tespit edildi: {e.AlertMessage}",
        cta: new EmailButtonModel { PrimaryText = "Hesap Güvenliğini İncele", PrimaryUrl = $"{Footer().Website}/account/security" },
        infoBox: new InformationBoxModel
        {
            Title = "Olay zamanı",
            Message = $"{e.OccurredAtUtc:dd MMMM yyyy HH:mm}{(e.IpAddress is null ? "" : $" — {e.IpAddress}")}",
            IconUrl = DefaultInfoIconUrl,
        },
        footer: Footer());
}

public sealed class AccountDeletedEmailBuilder(IOptions<EmailBrandOptions> brand) : EmailBuilderBase<AccountDeletedEvent>(brand)
{
    protected override EmailTemplateModel BuildModel(AccountDeletedEvent e) => EmailTemplateModel.Create(
        hero: null,
        title: "Hesabın silinme sürecinde",
        content: $"Merhaba {e.DisplayName}, hesabın silinme talebi alındı. Verilerin {e.PurgeDate:dd MMMM yyyy} tarihine kadar kalıcı olarak kaldırılacak.",
        cta: new EmailButtonModel { PrimaryText = "Vazgeçmek İstiyorum", PrimaryUrl = $"{Footer().Website}/account/restore" },
        infoBox: null,
        footer: Footer());
}
