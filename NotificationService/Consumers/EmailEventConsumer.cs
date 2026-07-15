namespace NotificationService.Consumers;

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Email.Contracts;
using NotificationService.Email.Events;
using NotificationService.Infrastructure;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

/// <summary>
/// Sorumluluğu yalnızca: RabbitMQ mesajını al -> deserialize et -> Email
/// Pipeline'a ilet. HTML üretmez, SMTP çağırmaz, template seçmez, string
/// replace yapmaz. Eski EmailConsumer.cs'in yerini alır; bağlantı-retry
/// mantığı (Docker startup için) korunmuştur.
/// </summary>
public sealed class EmailEventConsumer(
    IOptions<RabbitMqOptions> options,
    EmailEventTypeRegistry typeRegistry,
    IServiceScopeFactory scopeFactory,
    ILogger<EmailEventConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly RabbitMqOptions _options = options.Value;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.Username ?? "guest",
            Password = _options.Password ?? "guest",
        };

        // RabbitMQ bağlantısı hazır olana kadar bekle (Docker startup için).
        IConnection? connection = null;
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

        if (connection is null)
        {
            return;
        }

        _channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: _options.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: _options.PrefetchCount, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageReceivedAsync;

        await _channel.BasicConsumeAsync(
            queue: _options.Queue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation("EmailEventConsumer başladı. Kuyruk dinleniyor: {Queue}", _options.Queue);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs e)
    {
        var json = Encoding.UTF8.GetString(e.Body.Span);

        try
        {
            var envelope = JsonSerializer.Deserialize<EmailEventEnvelope>(json, JsonOptions)
                ?? throw new EmailPipelineException("Boş veya geçersiz mesaj zarfı.");

            if (!typeRegistry.TryResolve(envelope.EventType, out var eventType))
            {
                throw new EmailPipelineException($"Bilinmeyen event tipi: {envelope.EventType}");
            }

            var emailEvent = (EmailEventBase)envelope.Payload.Deserialize(eventType, JsonOptions)!;

            using var scope = scopeFactory.CreateScope();
            var pipeline = scope.ServiceProvider.GetRequiredService<IEmailPipeline>();
            await pipeline.ExecuteAsync(emailEvent, CancellationToken.None);

            await _channel!.BasicAckAsync(e.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            // NOT: Eski EmailConsumer requeue:true kullanıyordu (kalıcı hatalarda sonsuz
            // döngü riski taşır). Burada requeue:false (poison message drop) tercih edildi;
            // istersen bir retry-count / dead-letter exchange stratejisiyle değiştirebiliriz.
            logger.LogError(ex, "Email mesajı işlenemedi, mesaj reddediliyor (requeue: false). DeliveryTag: {DeliveryTag}", e.DeliveryTag);
            await _channel!.BasicNackAsync(e.DeliveryTag, multiple: false, requeue: false);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
        }

        await base.StopAsync(cancellationToken);
    }
}
