namespace CommunityPlatform.Domain.Entities;

public enum NotificationType
{
    // Workshop
    WorkshopPublished      = 0,
    WorkshopReminder       = 1,
    WorkshopCompleted      = 2,

    // Başvuru (enrollment)
    ApplicationReceived    = 10,
    ApplicationApproved    = 11,
    ApplicationRejected    = 12,

    // İçerik moderasyonu (Admin)
    ContentPendingApproval = 20,
    ContentApproved        = 21,
    ContentRejected        = 22,

    // Sosyal — yeni eklenenler
    NewFollower            = 30,
    PostLiked              = 31,
    PostCommented          = 32,
    PostShared             = 33,

    // Alan rezervasyonu (space booking)
    BookingRequested       = 40,
    BookingApproved        = 41,
    BookingRejected        = 42
}