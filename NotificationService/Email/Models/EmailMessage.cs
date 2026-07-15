namespace NotificationService.Email.Models;

/// <summary>Provider'a gönderilecek nihai, provider-agnostik e-posta zarfı.</summary>
public sealed class EmailMessage
{
    public required string ToEmail { get; init; }
    public string? ToName { get; init; }
    public required string Subject { get; init; }
    public required string HtmlBody { get; init; }
    public IReadOnlyList<EmailInlineImage>? InlineImages { get; init; }
    public IReadOnlyList<EmailAttachment>? Attachments { get; init; }
}

/// <summary>QR kod gibi HTML içinde cid: ile referans verilen gömülü görsel.</summary>
public sealed class EmailInlineImage
{
    public required string ContentId { get; init; }
    public required byte[] Content { get; init; }
    public required string MediaType { get; init; }
}

/// <summary>Fatura/sertifika PDF'i gibi dosya eki.</summary>
public sealed class EmailAttachment
{
    public required string FileName { get; init; }
    public required byte[] Content { get; init; }
    public required string MediaType { get; init; }
}
