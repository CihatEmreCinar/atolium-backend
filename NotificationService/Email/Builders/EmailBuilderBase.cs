namespace NotificationService.Email.Builders;

using Microsoft.Extensions.Options;
using NotificationService.Email.Contracts;
using NotificationService.Email.Events;
using NotificationService.Email.Models;
using NotificationService.Infrastructure;

/// <summary>
/// Tip-güvenli builder taban sınıfı. Ortak footer/brand alanlarını tek yerden
/// sağlar, böylece somut builder'lar yalnızca kendi içeriklerine odaklanır.
/// </summary>
public abstract class EmailBuilderBase<TEvent> : IEmailBuilder where TEvent : EmailEventBase
{
    private readonly EmailBrandOptions _brand;

    protected EmailBuilderBase(IOptions<EmailBrandOptions> brand) => _brand = brand.Value;

    public EmailTemplateModel Build(EmailEventBase emailEvent)
    {
        if (emailEvent is not TEvent typed)
        {
            throw new EmailPipelineException(
                $"{GetType().Name} yalnızca {typeof(TEvent).Name} tipini işleyebilir, ancak {emailEvent.GetType().Name} aldı.");
        }

        return BuildModel(typed);
    }

    protected abstract EmailTemplateModel BuildModel(TEvent emailEvent);

    protected FooterModel Footer() => new()
    {
        Website = _brand.Website,
        SupportEmail = _brand.SupportEmail,
        PrivacyUrl = _brand.PrivacyUrl,
        TermsUrl = _brand.TermsUrl,
    };

    protected string DefaultInfoIconUrl => _brand.InfoIconUrl;
}
