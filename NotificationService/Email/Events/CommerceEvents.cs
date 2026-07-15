namespace NotificationService.Email.Events;

using NotificationService.Email.TemplateResolver;

[EmailTemplate(EmailTemplateNames.Ticket)]
public sealed class TicketEvent : EmailEventBase
{
    public required string DisplayName { get; init; }
    public required string WorkshopTitle { get; init; }
    public required DateTimeOffset WorkshopStartsAtUtc { get; init; }
    public required string LocationName { get; init; }
    public required string TicketCode { get; init; }

    /// <summary>Önceden üretilmiş, imzalı QR kodun PNG baytları (mevcut QR üretim akışından).</summary>
    public required byte[] QrCodePng { get; init; }
}

[EmailTemplate(EmailTemplateNames.Invoice)]
public sealed class InvoiceEvent : EmailEventBase
{
    public required string DisplayName { get; init; }
    public required string InvoiceNumber { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required DateOnly DueDate { get; init; }
    public required string InvoiceUrl { get; init; }
}

[EmailTemplate(EmailTemplateNames.PaymentReceipt)]
public sealed class PaymentReceiptEvent : EmailEventBase
{
    public required string DisplayName { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required DateTimeOffset PaidAtUtc { get; init; }
    public required string ReceiptUrl { get; init; }
}
