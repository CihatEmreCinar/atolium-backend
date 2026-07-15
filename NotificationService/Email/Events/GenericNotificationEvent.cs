namespace NotificationService.Email.Events;

using NotificationService.Email.TemplateResolver;

/// <summary>
/// CommunityPlatform.Infrastructure.Services.NotificationService.NotifyAsync/
/// NotifyManyAsync ile birebir uyumlu, geriye dönük köprü event'i. Bugün
/// title/body serbest metin olarak geliyor ve tek tip email atılıyor;
/// bu event o akışı DEĞİŞTİRMEDEN Design System'e (Base.html) bağlar.
///
/// İleride belirli bir bildirim türü (ör. workshop onayı) için özel CTA/QR
/// gibi zengin içerik gerekiyorsa, o türe özel yeni bir Event + Builder
/// eklenip yalnızca o çağrı noktası GenericNotificationEvent yerine spesifik
/// event'i publish edecek şekilde güncellenir — geri kalan her şey aynı kalır.
/// </summary>
[EmailTemplate(EmailTemplateNames.Base)]
public sealed class GenericNotificationEvent : EmailEventBase
{
    public required string Title { get; init; }
    public required string Body { get; init; }
}
