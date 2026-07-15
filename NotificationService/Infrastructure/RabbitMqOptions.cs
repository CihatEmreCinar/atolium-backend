namespace NotificationService.Infrastructure;

/// <summary>RabbitMQ bağlantı ve kuyruk ayarları. Mevcut appsettings.json -> RabbitMQ ile birebir eşleşir.</summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string Queue { get; init; } = "notification.email";
    public ushort PrefetchCount { get; init; } = 1;
}
