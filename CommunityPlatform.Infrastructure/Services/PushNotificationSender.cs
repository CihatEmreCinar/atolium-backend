using System.Net.Http.Json;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CommunityPlatform.Infrastructure.Services;

public class PushNotificationSender(
    AppDbContext db,
    IHttpClientFactory httpClientFactory,
    ILogger<PushNotificationSender> logger) : IPushNotificationSender
{
    private const string ExpoPushUrl = "https://exp.host/--/api/v2/push/send";

    public Task SendAsync(Guid userId, string title, string body, object? data = null, CancellationToken ct = default)
        => SendManyAsync([userId], title, body, data, ct);

    public async Task SendManyAsync(IEnumerable<Guid> userIds, string title, string body, object? data = null, CancellationToken ct = default)
    {
        var idList = userIds.Distinct().ToList();
        if (idList.Count == 0) return;

        var tokens = await db.DeviceTokens
            .Where(t => idList.Contains(t.UserId))
            .ToListAsync(ct);

        if (tokens.Count == 0) return;

        var client = httpClientFactory.CreateClient();

        foreach (var token in tokens)
        {
            try
            {
                var response = await client.PostAsJsonAsync(ExpoPushUrl, new
                {
                    to = token.ExpoPushToken,
                    title,
                    body,
                    data
                }, ct);

                var responseBody = await response.Content.ReadAsStringAsync(ct);

                // ÖNEMLİ: Expo push API'si HTTP 200 dönse bile asıl teslimat hatası
                // (DeviceNotRegistered, InvalidCredentials, MessageTooBig vb.) response
                // body'sinin içinde "status":"error" olarak gömülü olur. Sadece status
                // code'a bakmak iOS'ta APNs key eksikliği gibi hataları tamamen gizler.
                if (!response.IsSuccessStatusCode || responseBody.Contains("\"status\":\"error\""))
                {
                    logger.LogWarning(
                        "Expo push teslim edilemedi. UserId={UserId} Platform={Platform} HttpStatus={Status} Body={Body}",
                        token.UserId, token.Platform, response.StatusCode, responseBody);
                }
            }
            catch (Exception ex)
            {
                // ReminderDispatchJob'daki desenle aynı — retry mantığı bilerek yok.
                logger.LogWarning(ex, "Expo push gönderilirken hata. UserId={UserId}", token.UserId);
            }
        }
    }
}