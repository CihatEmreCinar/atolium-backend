namespace NotificationService.Email.Builders;

using Microsoft.Extensions.Options;
using NotificationService.Email.Events;
using NotificationService.Email.Models;
using NotificationService.Infrastructure;

/// <summary>
/// Ticket, QR kodu HTML'e cid: ile gömmesi gerektiğinden EmailTemplateModel'in
/// zenginleştirilmiş bir alt sınıfını (TicketEmailTemplateModel) doğrudan
/// üretir ve kendi bespoke şablonunu (Templates/Ticket.html) kullanır.
/// </summary>
public sealed class TicketEmailBuilder(IOptions<EmailBrandOptions> brand) : EmailBuilderBase<TicketEvent>(brand)
{
    protected override EmailTemplateModel BuildModel(TicketEvent e)
    {
        var qrContentId = $"qr-{Guid.NewGuid():N}";
        var footer = Footer();

        return new TicketEmailTemplateModel
        {
            EmailTitle = "Biletin hazır",
            Content = $"Merhaba {e.DisplayName}, <strong>{e.WorkshopTitle}</strong> atölyesi için biletin aşağıdadır. Girişte QR kodunu göstermen yeterli.",
            WorkshopTitle = e.WorkshopTitle,
            WorkshopDate = e.WorkshopStartsAtUtc.ToString("dd MMMM yyyy, HH:mm"),
            LocationName = e.LocationName,
            TicketCode = e.TicketCode,
            QrContentId = qrContentId,
            InlineImages = [new EmailInlineImage { ContentId = qrContentId, Content = e.QrCodePng, MediaType = "image/png" }],
            Website = footer.Website,
            SupportEmail = footer.SupportEmail,
            PrivacyUrl = footer.PrivacyUrl,
            TermsUrl = footer.TermsUrl,
        };
    }
}

public sealed class InvoiceEmailBuilder(IOptions<EmailBrandOptions> brand) : EmailBuilderBase<InvoiceEvent>(brand)
{
    // NOT: Kalem bazlı bir fatura tablosu gerektiğinde EmailTemplateNames.Invoice,
    // Templates/Invoice.html'e yönlendirilebilir. Bugün Base.html ile çalışır.
    protected override EmailTemplateModel BuildModel(InvoiceEvent e) => EmailTemplateModel.Create(
        hero: null,
        title: $"Fatura #{e.InvoiceNumber}",
        content: $"Merhaba {e.DisplayName}, <strong>{e.Amount:0.00} {e.Currency}</strong> tutarındaki faturan hazır. Son ödeme tarihi: {e.DueDate:dd MMMM yyyy}.",
        cta: new EmailButtonModel { PrimaryText = "Faturayı Görüntüle", PrimaryUrl = e.InvoiceUrl },
        infoBox: null,
        footer: Footer());
}

public sealed class PaymentReceiptEmailBuilder(IOptions<EmailBrandOptions> brand) : EmailBuilderBase<PaymentReceiptEvent>(brand)
{
    protected override EmailTemplateModel BuildModel(PaymentReceiptEvent e) => EmailTemplateModel.Create(
        hero: null,
        title: "Ödemen alındı",
        content: $"Merhaba {e.DisplayName}, <strong>{e.Amount:0.00} {e.Currency}</strong> tutarındaki ödemen {e.PaidAtUtc:dd MMMM yyyy, HH:mm} tarihinde başarıyla alındı.",
        cta: new EmailButtonModel { PrimaryText = "Makbuzu Görüntüle", PrimaryUrl = e.ReceiptUrl },
        infoBox: null,
        footer: Footer());
}
