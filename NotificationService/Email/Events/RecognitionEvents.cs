namespace NotificationService.Email.Events;

using NotificationService.Email.TemplateResolver;

[EmailTemplate(EmailTemplateNames.Certificate)]
public sealed class CertificateEvent : EmailEventBase
{
    public required string DisplayName { get; init; }
    public required string WorkshopTitle { get; init; }
    public required DateOnly CompletionDate { get; init; }
    public required string CertificateUrl { get; init; }
}

[EmailTemplate(EmailTemplateNames.Achievement)]
public sealed class AchievementEvent : EmailEventBase
{
    public required string DisplayName { get; init; }
    public required string AchievementTitle { get; init; }
    public required string AchievementDescription { get; init; }
    public required string BadgeIconUrl { get; init; }
}
