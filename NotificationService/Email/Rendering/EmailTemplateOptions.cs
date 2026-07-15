namespace NotificationService.Email.Rendering;

/// <summary>Template dosyalarının bulunduğu dizin ayarı. appsettings.json -> Email:Templates.</summary>
public sealed class EmailTemplateOptions
{
    public const string SectionName = "Email:Templates";

    /// <summary>Mutlak veya proje köküne (ContentRootPath) göre göreli yol.</summary>
    public string TemplatesDirectory { get; init; } = "Email/Templates";
}
