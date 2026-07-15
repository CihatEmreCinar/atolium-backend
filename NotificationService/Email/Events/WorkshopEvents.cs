namespace NotificationService.Email.Events;

using NotificationService.Email.TemplateResolver;

[EmailTemplate(EmailTemplateNames.WorkshopRegistration)]
public sealed class WorkshopRegistrationEvent : EmailEventBase
{
    public required string DisplayName { get; init; }
    public required string WorkshopTitle { get; init; }
    public required DateTimeOffset WorkshopStartsAtUtc { get; init; }
    public required string OrganizerName { get; init; }
}

[EmailTemplate(EmailTemplateNames.WorkshopApproved)]
public sealed class WorkshopApprovedEvent : EmailEventBase
{
    public required string DisplayName { get; init; }
    public required string WorkshopTitle { get; init; }
    public required DateTimeOffset WorkshopStartsAtUtc { get; init; }
    public required string TicketUrl { get; init; }
}

[EmailTemplate(EmailTemplateNames.WorkshopReminder)]
public sealed class WorkshopReminderEvent : EmailEventBase
{
    public required string DisplayName { get; init; }
    public required string WorkshopTitle { get; init; }
    public required DateTimeOffset WorkshopStartsAtUtc { get; init; }
    public required string LocationName { get; init; }
}

[EmailTemplate(EmailTemplateNames.WorkshopCancelled)]
public sealed class WorkshopCancelledEvent : EmailEventBase
{
    public required string DisplayName { get; init; }
    public required string WorkshopTitle { get; init; }
    public required string Reason { get; init; }
    public string? RefundInfo { get; init; }
}

[EmailTemplate(EmailTemplateNames.Waitlist)]
public sealed class WaitlistEvent : EmailEventBase
{
    public required string DisplayName { get; init; }
    public required string WorkshopTitle { get; init; }
    public required int WaitlistPosition { get; init; }
}
