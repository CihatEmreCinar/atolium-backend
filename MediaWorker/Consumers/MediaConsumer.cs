using System.Text;
using System.Text.Json;
using CommunityPlatform.Application.DTOs.Media;
using MediaWorker.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MediaWorker.Consumers;

public class MediaConsumer(
    IConfiguration config,
    IServiceScopeFactory scopeFactory,
    ILogger<MediaConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitConfig = config.GetSection("RabbitMQ");
        var queue = rabbitConfig["MediaQueue"] ?? "media.processing";

        var factory = new ConnectionFactory
        {
            HostName = rabbitConfig["Host"] ?? "localhost",
            Port     = int.Parse(rabbitConfig["Port"] ?? "5672"),
            UserName = rabbitConfig["Username"] ?? "guest",
            Password = rabbitConfig["Password"] ?? "guest"
        };

        IConnection connection = null!;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                connection = await factory.CreateConnectionAsync(stoppingToken);
                logger.LogInformation("RabbitMQ bağlantısı kuruldu.");
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning("RabbitMQ bağlanamadı, 5 sn sonra tekrar deneniyor... {Message}", ex.Message);
                await Task.Delay(5000, stoppingToken);
            }
        }

        if (connection == null) return;

        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());

            try
            {
                var message = JsonSerializer.Deserialize<MediaUploadedEvent>(body, JsonOptions);

                if (message != null)
                {
                    // Her mesaj kendi DI scope'unda — AppDbContext scoped, tek consumer
                    // thread'i boyunca birikip tutarsızlığa yol açmasın diye.
                    using var scope = scopeFactory.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<MediaProcessor>();
                    await processor.ProcessAsync(message, stoppingToken);
                }

                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                // Doğrulama/decode hataları MediaProcessor içinde Failed'e çekilip
                // ack'leniyor — buraya düşen yalnızca altyapısal (DB/MinIO erişilemedi vb.)
                // hatalar, bunlar için requeue mantıklı.
                logger.LogError(ex, "Medya işlenemedi, kuyrukta bırakılıyor...");
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        await channel.BasicConsumeAsync(
            queue: queue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation("MediaConsumer başladı. Kuyruk dinleniyor: {Queue}", queue);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}