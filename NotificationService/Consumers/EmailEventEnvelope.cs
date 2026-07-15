namespace NotificationService.Consumers;

using System.Text.Json;

/// <summary>
/// RabbitMQ üzerinden taşınan zarf. Payload, EventType alanına göre ilgili
/// concrete event tipine deserialize edilir. Örnek mesaj gövdesi:
/// { "EventType": "VerifyEmailEvent", "Payload": { "ToEmail": "...", ... } }
/// </summary>
public sealed class EmailEventEnvelope
{
    public required string EventType { get; init; }
    public required JsonElement Payload { get; init; }
}
