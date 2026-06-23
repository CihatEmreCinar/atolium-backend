using System.Text;
using System.Text.Json;
using NotificationService.Models;
using NotificationService.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationService.Consumers;

public class EmailConsumer(
    IConfiguration config,
    EmailSenderService emailSender,
    ILogger<EmailConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitConfig = config.GetSection("RabbitMQ");
        var queue = rabbitConfig["Queue"] ?? "notification.email";

        var factory = new ConnectionFactory
        {
            HostName = rabbitConfig["Host"] ?? "localhost",
            Port     = int.Parse(rabbitConfig["Port"] ?? "5672"),
            UserName = rabbitConfig["Username"] ?? "guest",
            Password = rabbitConfig["Password"] ?? "guest"
        };

        // RabbitMQ bağlantısı hazır olana kadar bekle (Docker startup için)
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

        // Kuyruk yoksa oluştur (API ile aynı parametreler)
        await channel.QueueDeclareAsync(
            queue: queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        // Bir seferde 1 mesaj al — işlenince bir sonrakini al
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());

            try
            {
                var message = JsonSerializer.Deserialize<EmailMessage>(body, JsonOptions);

                if (message != null)
                    await emailSender.SendAsync(message);

                // Başarılı → mesajı kuyruktan kaldır
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Email işlenemedi, kuyrukta bırakılıyor...");

                // Başarısız → mesajı kuyruğa geri koy (requeue: true)
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        await channel.BasicConsumeAsync(
            queue: queue,
            autoAck: false,   // manuel ack — garantili teslim
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation("EmailConsumer başladı. Kuyruk dinleniyor: {Queue}", queue);

        // Uygulama durduğunda çıkış
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
