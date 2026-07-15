namespace NotificationService.Email.TemplateResolver;

using System.Collections.Concurrent;
using System.Reflection;
using NotificationService.Email.Contracts;
using NotificationService.Infrastructure;

/// <summary>
/// [EmailTemplate] attribute'unu okuyarak event Type -> template adı eşlemesi
/// yapar. HTML render etmez, yalnızca isim çözümler. Sonuçlar süreç ömrü
/// boyunca cache'lenir (reflection her seferinde tekrar çalışmaz).
/// </summary>
public sealed class DefaultTemplateResolver : ITemplateResolver
{
    private static readonly ConcurrentDictionary<Type, string> Cache = new();

    public string Resolve(Type eventType)
    {
        return Cache.GetOrAdd(eventType, static t =>
        {
            var attribute = t.GetCustomAttribute<EmailTemplateAttribute>()
                ?? throw new EmailPipelineException(
                    $"'{t.Name}' üzerinde [EmailTemplate] attribute tanımlı değil. " +
                    "Her email event sınıfı bir template'e bağlanmalıdır.");

            return attribute.TemplateName;
        });
    }
}
