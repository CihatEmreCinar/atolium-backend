namespace NotificationService.Infrastructure;

/// <summary>Tüm template'lerde ortak kullanılan marka/footer bilgileri. appsettings.json -> Email:Brand.</summary>
public sealed class EmailBrandOptions
{
    public const string SectionName = "Email:Brand";

    public string Website { get; init; } = "https://atolium.com";
    public string SupportEmail { get; init; } = "destek@atolium.com";
    public string PrivacyUrl { get; init; } = "https://atolium.com/privacy";
    public string TermsUrl { get; init; } = "https://atolium.com/terms";
    public string InfoIconUrl { get; init; } = "https://cdn.atolium.com/email/assets/icons/info.png";
}
