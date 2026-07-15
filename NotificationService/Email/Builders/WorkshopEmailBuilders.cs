namespace NotificationService.Email.Builders;

using Microsoft.Extensions.Options;
using NotificationService.Email.Events;
using NotificationService.Email.Models;
using NotificationService.Infrastructure;

public sealed class WorkshopRegistrationEmailBuilder(IOptions<EmailBrandOptions> brand) : EmailBuilderBase<WorkshopRegistrationEvent>(brand)
{
    protected override EmailTemplateModel BuildModel(WorkshopRegistrationEvent e) => EmailTemplateModel.Create(
        hero: null,
        title: "Kayıt talebin alındı",
        content: $"Merhaba {e.DisplayName}, <strong>{e.WorkshopTitle}</strong> atölyesine ({e.WorkshopStartsAtUtc:dd MMMM yyyy, HH:mm}) kayıt talebin {e.OrganizerName} tarafından inceleniyor. Onaylandığında sana haber vereceğiz.",
        cta: null,
        infoBox: null,
        footer: Footer());
}

public sealed class WorkshopApprovedEmailBuilder(IOptions<EmailBrandOptions> brand) : EmailBuilderBase<WorkshopApprovedEvent>(brand)
{
    protected override EmailTemplateModel BuildModel(WorkshopApprovedEvent e) => EmailTemplateModel.Create(
        hero: null,
        title: "Kaydın onaylandı!",
        content: $"Merhaba {e.DisplayName}, <strong>{e.WorkshopTitle}</strong> atölyesine katılımın onaylandı. Atölye {e.WorkshopStartsAtUtc:dd MMMM yyyy, HH:mm} tarihinde gerçekleşecek.",
        cta: new EmailButtonModel { PrimaryText = "Biletimi Görüntüle", PrimaryUrl = e.TicketUrl },
        infoBox: null,
        footer: Footer());
}

public sealed class WorkshopReminderEmailBuilder(IOptions<EmailBrandOptions> brand) : EmailBuilderBase<WorkshopReminderEvent>(brand)
{
    protected override EmailTemplateModel BuildModel(WorkshopReminderEvent e) => EmailTemplateModel.Create(
        hero: null,
        title: "Atölyen yaklaşıyor",
        content: $"Merhaba {e.DisplayName}, <strong>{e.WorkshopTitle}</strong> atölyen {e.WorkshopStartsAtUtc:dd MMMM yyyy, HH:mm} tarihinde, {e.LocationName} adresinde başlayacak. Seni orada görmek için sabırsızlanıyoruz!",
        cta: null,
        infoBox: null,
        footer: Footer());
}

public sealed class WorkshopCancelledEmailBuilder(IOptions<EmailBrandOptions> brand) : EmailBuilderBase<WorkshopCancelledEvent>(brand)
{
    protected override EmailTemplateModel BuildModel(WorkshopCancelledEvent e) => EmailTemplateModel.Create(
        hero: null,
        title: "Atölye iptal edildi",
        content: $"Merhaba {e.DisplayName}, üzülerek bildiririz ki <strong>{e.WorkshopTitle}</strong> atölyesi iptal edildi. Sebep: {e.Reason}",
        cta: null,
        infoBox: e.RefundInfo is null ? null : new InformationBoxModel
        {
            Title = "İade bilgisi",
            Message = e.RefundInfo,
            IconUrl = DefaultInfoIconUrl,
        },
        footer: Footer());
}

public sealed class WaitlistEmailBuilder(IOptions<EmailBrandOptions> brand) : EmailBuilderBase<WaitlistEvent>(brand)
{
    protected override EmailTemplateModel BuildModel(WaitlistEvent e) => EmailTemplateModel.Create(
        hero: null,
        title: "Bekleme listesindesin",
        content: $"Merhaba {e.DisplayName}, <strong>{e.WorkshopTitle}</strong> atölyesi için bekleme listesinde {e.WaitlistPosition}. sıradasın. Bir yer açıldığında seni hemen bilgilendireceğiz.",
        cta: null,
        infoBox: null,
        footer: Footer());
}
