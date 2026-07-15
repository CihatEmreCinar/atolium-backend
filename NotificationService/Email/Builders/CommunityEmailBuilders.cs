namespace NotificationService.Email.Builders;

using Microsoft.Extensions.Options;
using NotificationService.Email.Events;
using NotificationService.Email.Models;
using NotificationService.Infrastructure;

public sealed class CommunityInvitationEmailBuilder(IOptions<EmailBrandOptions> brand) : EmailBuilderBase<CommunityInvitationEvent>(brand)
{
    protected override EmailTemplateModel BuildModel(CommunityInvitationEvent e) => EmailTemplateModel.Create(
        hero: null,
        title: "Bir topluluğa davet edildin",
        content: $"Merhaba {e.DisplayName}, {e.InviterName} seni <strong>{e.CommunityName}</strong> topluluğuna davet etti.",
        cta: new EmailButtonModel { PrimaryText = "Daveti Görüntüle", PrimaryUrl = e.InviteUrl },
        infoBox: null,
        footer: Footer());
}

public sealed class OrganizerMessageEmailBuilder(IOptions<EmailBrandOptions> brand) : EmailBuilderBase<OrganizerMessageEvent>(brand)
{
    protected override EmailTemplateModel BuildModel(OrganizerMessageEvent e) => EmailTemplateModel.Create(
        hero: null,
        title: $"{e.OrganizerName} bir mesaj gönderdi",
        content: $"<strong>{e.WorkshopTitle}</strong> atölyesi hakkında:<br><br>{e.MessageBody}",
        cta: null,
        infoBox: null,
        footer: Footer());
}

public sealed class NewsletterEmailBuilder(IOptions<EmailBrandOptions> brand) : EmailBuilderBase<NewsletterEvent>(brand)
{
    // NOT: Çoklu makale/çok bölümlü bir bülten tasarımı gerektiğinde
    // EmailTemplateNames.Newsletter, Templates/Newsletter.html'e yönlendirilip
    // bu builder o modele göre genişletilebilir. Bugün Base.html ile tam çalışır.
    protected override EmailTemplateModel BuildModel(NewsletterEvent e) => EmailTemplateModel.Create(
        hero: null,
        title: e.Headline,
        content: $"Merhaba {e.DisplayName}, {e.Summary}",
        cta: new EmailButtonModel { PrimaryText = "Devamını Oku", PrimaryUrl = e.ReadMoreUrl },
        infoBox: null,
        footer: Footer(),
        showUnsubscribe: true,
        unsubscribeUrl: $"{Footer().Website}/newsletter/unsubscribe");
}

/// <summary>Mevcut NotifyAsync(title, body, sendEmail) akışı için geriye dönük köprü builder'ı.</summary>
public sealed class GenericNotificationEmailBuilder(IOptions<EmailBrandOptions> brand) : EmailBuilderBase<GenericNotificationEvent>(brand)
{
    protected override EmailTemplateModel BuildModel(GenericNotificationEvent e) => EmailTemplateModel.Create(
        hero: null,
        title: e.Title,
        content: e.Body,
        cta: null,
        infoBox: null,
        footer: Footer());
}
