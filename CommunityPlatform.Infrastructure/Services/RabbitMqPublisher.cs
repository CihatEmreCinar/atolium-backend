using System.Text;
using System.Text.Json;
using CommunityPlatform.Application.Interfaces;
using RabbitMQ.Client;

namespace CommunityPlatform.Infrastructure.Services;

public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RabbitMqPublisher(IConnection connection)
    {
        _connection = connection;
        _channel = connection.CreateChannelAsync().GetAwaiter().GetResult();
    }

    public async Task PublishAsync<T>(string queue, T message)
    {
        await _channel.QueueDeclareAsync(
            queue: queue,
            durable: true,       // broker restart sonrası kuyruk korunur
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, JsonOptions));

        var props = new BasicProperties
        {
            Persistent = true,   // mesaj disk'e yazılır, kaybolmaz
            ContentType = "application/json"
        };

        await _channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queue,
            mandatory: false,
            basicProperties: props,
            body: body);
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}
