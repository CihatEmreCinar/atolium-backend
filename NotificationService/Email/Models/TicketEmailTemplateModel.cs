namespace NotificationService.Email.Models;

/// <summary>
/// Ticket.html şablonuna özgü, QR kodu inline (cid:) görsel olarak taşıyan
/// zenginleştirilmiş model. EmailPipeline, IHasInlineImages implementasyonunu
/// otomatik olarak EmailMessage.InlineImages'a taşır.
/// </summary>
public sealed class TicketEmailTemplateModel : EmailTemplateModel, IHasInlineImages
{
    public required string WorkshopTitle { get; init; }
    public required string WorkshopDate { get; init; }
    public required string LocationName { get; init; }
    public required string TicketCode { get; init; }
    public required string QrContentId { get; init; }

    public required IReadOnlyList<EmailInlineImage> InlineImages { get; init; }
}
