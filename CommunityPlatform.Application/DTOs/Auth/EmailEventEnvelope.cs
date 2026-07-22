namespace CommunityPlatform.Application.DTOs.Auth;

/// <summary>NotificationService'in beklediği RabbitMQ e-posta zarfı.</summary>
public record EmailEventEnvelope(string EventType, object Payload);
