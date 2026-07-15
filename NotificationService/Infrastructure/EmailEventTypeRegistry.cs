namespace NotificationService.Infrastructure;

using NotificationService.Email.Events;

/// <summary>
/// RabbitMQ zarfındaki "EventType" alanını (ör. "VerifyEmailEvent") karşılık
/// gelen CLR Type'ına eşler. Assembly'i bir kez tarar ve cache'ler; yeni bir
/// event eklendiğinde bu sınıfta HİÇBİR değişiklik gerekmez (otomatik keşif).
/// </summary>
public sealed class EmailEventTypeRegistry
{
    private readonly IReadOnlyDictionary<string, Type> _typesByName;

    public EmailEventTypeRegistry()
    {
        _typesByName = typeof(EmailEventBase).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsClass: true } && typeof(EmailEventBase).IsAssignableFrom(t))
            .ToDictionary(t => t.Name, t => t);
    }

    public bool TryResolve(string eventTypeName, out Type eventType) =>
        _typesByName.TryGetValue(eventTypeName, out eventType!);
}
