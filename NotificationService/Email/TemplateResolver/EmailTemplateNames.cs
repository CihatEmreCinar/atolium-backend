namespace NotificationService.Email.TemplateResolver;

/// <summary>
/// Tüm template dosya adlarının merkezi, magic-string-free tanımı.
///
/// Şu an "Base.html", Title/Content/CTA/InfoBox alanlarına sahip 19 standart
/// e-posta tipini karşılar. Ticket, QR kod gömme ihtiyacı nedeniyle kendi
/// bespoke template'ine (Ticket.html) sahiptir.
///
/// Invoice, PaymentReceipt, Certificate, Achievement ve Newsletter için de
/// aynı yaklaşım geçerlidir: bugün Base.html ile (generic ama tam çalışır
/// şekilde) render edilirler; ileride kalem tablosu / rozet / çoklu makale
/// gibi kendine özgü bir görsele ihtiyaç duyduklarında, yalnızca burada
/// ilgili sabiti yeni bir .html dosyasına işaret edecek şekilde güncelleyip
/// Templates/ klasörüne o dosyayı eklemek yeterlidir. Consumer, Pipeline ve
/// Provider hiçbir şekilde değişmez.
/// </summary>
public static class EmailTemplateNames
{
    public const string Base = "Base.html";

    public const string Welcome = Base;
    public const string VerifyEmail = Base;
    public const string MagicLink = Base;
    public const string PasswordReset = Base;
    public const string PasswordChanged = Base;
    public const string SecurityAlert = Base;
    public const string AccountDeleted = Base;

    public const string WorkshopRegistration = Base;
    public const string WorkshopApproved = Base;
    public const string WorkshopReminder = Base;
    public const string WorkshopCancelled = Base;
    public const string Waitlist = Base;

    public const string CommunityInvitation = Base;
    public const string OrganizerMessage = Base;
    public const string Newsletter = Base;

    public const string Ticket = "Ticket.html";
    public const string Invoice = Base;
    public const string PaymentReceipt = Base;

    public const string Certificate = Base;
    public const string Achievement = Base;
}
