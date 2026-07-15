namespace NotificationService.Email.Contracts;

/// <summary>
/// Bir email event tipi için kullanılacak template dosya adını belirler.
/// HTML render etmez, yalnızca isim çözümlemesi yapar.
/// </summary>
public interface ITemplateResolver
{
    string Resolve(Type eventType);
}
