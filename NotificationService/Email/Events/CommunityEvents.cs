namespace NotificationService.Email.Events;

using NotificationService.Email.TemplateResolver;

[EmailTemplate(EmailTemplateNames.CommunityInvitation)]
public sealed class CommunityInvitationEvent : EmailEventBase
{
    public required string DisplayName { get; init; }
    public required string InviterName { get; init; }
    public required string CommunityName { get; init; }
    public required string InviteUrl { get; init; }
}

[EmailTemplate(EmailTemplateNames.OrganizerMessage)]
public sealed class OrganizerMessageEvent : EmailEventBase
{
    public required string DisplayName { get; init; }
    public required string OrganizerName { get; init; }
    public required string WorkshopTitle { get; init; }
    public required string MessageBody { get; init; }
}

[EmailTemplate(EmailTemplateNames.Newsletter)]
public sealed class NewsletterEvent : EmailEventBase
{
    public required string DisplayName { get; init; }
    public required string Headline { get; init; }
    public required string Summary { get; init; }
    public required string ReadMoreUrl { get; init; }
}
