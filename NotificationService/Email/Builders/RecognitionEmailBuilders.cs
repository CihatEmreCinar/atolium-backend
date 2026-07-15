namespace NotificationService.Email.Builders;

using Microsoft.Extensions.Options;
using NotificationService.Email.Events;
using NotificationService.Email.Models;
using NotificationService.Infrastructure;

public sealed class CertificateEmailBuilder(IOptions<EmailBrandOptions> brand) : EmailBuilderBase<CertificateEvent>(brand)
{
    protected override EmailTemplateModel BuildModel(CertificateEvent e) => EmailTemplateModel.Create(
        hero: null,
        title: "Sertifikan hazır!",
        content: $"Tebrikler {e.DisplayName}! <strong>{e.WorkshopTitle}</strong> atölyesini {e.CompletionDate:dd MMMM yyyy} tarihinde tamamladın. Sertifikan aşağıdan indirilebilir.",
        cta: new EmailButtonModel { PrimaryText = "Sertifikamı İndir", PrimaryUrl = e.CertificateUrl },
        infoBox: null,
        footer: Footer());
}

public sealed class AchievementEmailBuilder(IOptions<EmailBrandOptions> brand) : EmailBuilderBase<AchievementEvent>(brand)
{
    protected override EmailTemplateModel BuildModel(AchievementEvent e) => EmailTemplateModel.Create(
        hero: new HeroModel { IconUrl = e.BadgeIconUrl },
        title: $"Yeni bir rozet kazandın: {e.AchievementTitle}",
        content: $"Tebrikler {e.DisplayName}! {e.AchievementDescription}",
        cta: null,
        infoBox: null,
        footer: Footer());
}
