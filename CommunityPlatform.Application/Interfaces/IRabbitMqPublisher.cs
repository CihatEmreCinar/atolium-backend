namespace CommunityPlatform.Application.Interfaces;

public interface IRabbitMqPublisher
{
    Task PublishAsync<T>(string queue, T message);
}
