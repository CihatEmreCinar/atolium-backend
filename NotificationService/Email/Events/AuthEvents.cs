namespace NotificationService.Email.Events;

using NotificationService.Email.TemplateResolver;

[EmailTemplate(EmailTemplateNames.Welcome)]
public sealed class WelcomeEvent : EmailEventBase
{
    public required string DisplayName { get; init; }
}

[EmailTemplate(EmailTemplateNames.VerifyEmail)]
public sealed class VerifyEmailEvent : EmailEventBase
{
    public required string DisplayName { get; init; }
    public required string VerificationUrl { get; init; }
    public required string VerificationCode { get; init; }
    public TimeSpan ExpiresIn { get; init; } = TimeSpan.FromHours(24);
    public TimeSpan OtpExpiresIn { get; init; } = TimeSpan.FromMinutes(10);
}

[EmailTemplate(EmailTemplateNames.MagicLink)]
public sealed class MagicLinkEvent : EmailEventBase
{
    public required string DisplayName { get; init; }
    public required string MagicLinkUrl { get; init; }
    public TimeSpan ExpiresIn { get; init; } = TimeSpan.FromMinutes(15);
}

[EmailTemplate(EmailTemplateNames.PasswordReset)]
public sealed class PasswordResetEvent : EmailEventBase
{
    public required string DisplayName { get; init; }
    public required string ResetUrl { get; init; }
    public TimeSpan ExpiresIn { get; init; } = TimeSpan.FromHours(1);
}

[EmailTemplate(EmailTemplateNames.PasswordChanged)]
public sealed class PasswordChangedEvent : EmailEventBase
{
    public required string DisplayName { get; init; }
    public required DateTimeOffset ChangedAtUtc { get; init; }
    public string? IpAddress { get; init; }
}

[EmailTemplate(EmailTemplateNames.SecurityAlert)]
public sealed class SecurityAlertEvent : EmailEventBase
{
    public required string DisplayName { get; init; }
    public required string AlertMessage { get; init; }
    public required DateTimeOffset OccurredAtUtc { get; init; }
    public string? IpAddress { get; init; }
}

[EmailTemplate(EmailTemplateNames.AccountDeleted)]
public sealed class AccountDeletedEvent : EmailEventBase
{
    public required string DisplayName { get; init; }
    public required DateOnly PurgeDate { get; init; }
}
