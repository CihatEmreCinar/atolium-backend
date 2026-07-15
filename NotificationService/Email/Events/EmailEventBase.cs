namespace NotificationService.Email.Events;

/// <summary>
/// Tüm email business event'lerinin ortak temeli. Bu sınıflar RabbitMQ
/// mesajının payload'ından deserialize edilir (bkz. Consumers/EmailEventEnvelope).
/// </summary>
public abstract class EmailEventBase
{
    public required string ToEmail { get; init; }
    public string? ToName { get; init; }
}
