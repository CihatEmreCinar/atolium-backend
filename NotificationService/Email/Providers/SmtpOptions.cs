namespace NotificationService.Email.Providers;

/// <summary>SMTP bağlantı ayarları. Mevcut appsettings.json -> Smtp ile birebir eşleşir.
/// Username/Password'ü appsettings.json'a YAZMA — mevcut projede zaten
/// UserSecretsId tanımlı, `dotnet user-secrets set "Smtp:Username" ...` kullan.</summary>
public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public required string Host { get; init; }
    public int Port { get; init; } = 587;
    public string? Username { get; init; }
    public string? Password { get; init; }

    /// <summary>true: SSL/TLS (port 465). false: STARTTLS (port 587 — Gmail dahil çoğu sağlayıcının varsayılanı).</summary>
    public bool UseSsl { get; init; } = false;

    public required string FromAddress { get; init; }
    public string FromName { get; init; } = "Atolium";
}
