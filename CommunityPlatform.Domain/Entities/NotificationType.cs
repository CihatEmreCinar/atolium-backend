namespace CommunityPlatform.Domain.Entities;

public enum NotificationType
{
    WorkshopPublished,
    WorkshopReminder,
    WorkshopCompleted,
    ApplicationReceived,
    ApplicationApproved,
    ApplicationRejected,
    ContentPendingApproval,
    ContentApproved,
    ContentRejected
}