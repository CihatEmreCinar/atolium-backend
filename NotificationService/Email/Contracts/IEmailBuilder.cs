namespace NotificationService.Email.Contracts;

using NotificationService.Email.Events;
using NotificationService.Email.Models;

/// <summary>
/// Business event'i email render modeline dönüştürür. HTML üretmez.
/// Her somut implementasyon, ilgili event Type'ını key alan bir keyed
/// DI servisi olarak kaydedilir (bkz. Infrastructure/ServiceCollectionExtensions).
/// </summary>
public interface IEmailBuilder
{
    EmailTemplateModel Build(EmailEventBase emailEvent);
}
