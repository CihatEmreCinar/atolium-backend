namespace NotificationService.Email.Models;

/// <summary>Tüm template'lerde ortak olan marka/footer alanları.</summary>
public sealed class FooterModel
{
    public required string Website { get; init; }
    public required string SupportEmail { get; init; }
    public required string PrivacyUrl { get; init; }
    public required string TermsUrl { get; init; }
}
