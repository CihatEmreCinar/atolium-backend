namespace NotificationService.Email.Models;

/// <summary>
/// Render katmanına geçilecek nihai model. Alan adları kasıtlı olarak
/// Templates/Base.html içindeki Handlebars token'larıyla birebir eşleşir.
/// Bu bir business entity DEĞİLDİR — yalnızca render amaçlıdır.
/// Zenginleştirilmiş şablonlar (ör. Ticket) bu sınıftan türeyip ek alan
/// ve/veya IHasInlineImages ekleyebilir.
/// </summary>
public class EmailTemplateModel
{
    public required string EmailTitle { get; init; }

    /// <summary>Basit HTML (ör. &lt;strong&gt;, &lt;br&gt;) içerebilir; template'te {{{Content}}} olarak (escape edilmeden) render edilir.</summary>
    public required string Content { get; init; }

    public string? HeroIconUrl { get; init; }

    public string? ButtonUrl { get; init; }
    public string? ButtonText { get; init; }
    public string? SecondaryButtonUrl { get; init; }
    public string? SecondaryButtonText { get; init; }

    public string? AdditionalInformation { get; init; }
    public string? InfoTitle { get; init; }
    public string? InfoIconUrl { get; init; }

    public bool ShowUnsubscribe { get; init; }
    public string? UnsubscribeUrl { get; init; }

    // --- Ortak marka / footer alanları: EmailBuilderBase.Footer() tarafından doldurulur ---
    public required string Website { get; init; }
    public required string SupportEmail { get; init; }
    public required string PrivacyUrl { get; init; }
    public required string TermsUrl { get; init; }
    public int CurrentYear { get; init; } = DateTime.UtcNow.Year;

    /// <summary>
    /// Standart (Title/Content/CTA/InfoBox) e-posta tipleri için kısayol factory.
    /// Ticket gibi yapısal olarak farklı şablonlar kendi türetilmiş modellerini
    /// doğrudan constructor ile oluşturur.
    /// </summary>
    public static EmailTemplateModel Create(
        HeroModel? hero,
        string title,
        string content,
        EmailButtonModel? cta,
        InformationBoxModel? infoBox,
        FooterModel footer,
        bool showUnsubscribe = false,
        string? unsubscribeUrl = null) => new()
    {
        EmailTitle = title,
        Content = content,
        HeroIconUrl = hero?.IconUrl,
        ButtonUrl = cta?.PrimaryUrl,
        ButtonText = cta?.PrimaryText,
        SecondaryButtonUrl = cta?.SecondaryUrl,
        SecondaryButtonText = cta?.SecondaryText,
        AdditionalInformation = infoBox?.Message,
        InfoTitle = infoBox?.Title,
        InfoIconUrl = infoBox?.IconUrl,
        ShowUnsubscribe = showUnsubscribe,
        UnsubscribeUrl = unsubscribeUrl,
        Website = footer.Website,
        SupportEmail = footer.SupportEmail,
        PrivacyUrl = footer.PrivacyUrl,
        TermsUrl = footer.TermsUrl,
    };
}
