namespace NotificationService.Email.Models;

/// <summary>
/// QR kod gibi cid: ile gömülen inline görsel gerektiren email modelleri
/// bu arayüzü uygular; EmailPipeline bu görselleri otomatik olarak
/// EmailMessage'a taşır.
/// </summary>
public interface IHasInlineImages
{
    IReadOnlyList<EmailInlineImage> InlineImages { get; }
}
