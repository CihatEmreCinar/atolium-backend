using CommunityPlatform.Application.DTOs.DeviceTokens;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityPlatform.API.Controllers;

[ApiController]
[Route("api/v1/device-tokens")]
[Authorize]
public class DeviceTokensController(
    AppDbContext db,
    ICurrentUserService currentUser) : ControllerBase
{
    private static readonly string[] AllowedPlatforms = ["ios", "android"];

    // Push bildirim almak için cihaz token'ını kaydeder (upsert).
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterDeviceTokenRequest request)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.ExpoPushToken))
            return BadRequest(new { message = "expoPushToken zorunludur." });

        if (!AllowedPlatforms.Contains(request.Platform))
            return BadRequest(new { message = "platform 'ios' veya 'android' olmalı." });

        var existing = await db.DeviceTokens.FirstOrDefaultAsync(t => t.ExpoPushToken == request.ExpoPushToken);

        if (existing == null)
        {
            db.DeviceTokens.Add(new DeviceToken
            {
                UserId = currentUser.UserId.Value,
                ExpoPushToken = request.ExpoPushToken,
                Platform = request.Platform
            });
        }
        else
        {
            existing.UserId = currentUser.UserId.Value;
            existing.Platform = request.Platform;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();

        return Ok(new { message = "Cihaz token'ı kaydedildi." });
    }

    // Doğrulama amaçlı: mevcut kullanıcının kayıtlı cihaz token'ları.
    [HttpGet]
    public async Task<IActionResult> GetMine()
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var tokens = await db.DeviceTokens
            .Where(t => t.UserId == currentUser.UserId.Value)
            .Select(t => new { t.Id, t.ExpoPushToken, t.Platform, t.CreatedAt, t.UpdatedAt })
            .ToListAsync();

        return Ok(tokens);
    }

    // TEŞHİS AMAÇLI: kendi kayıtlı token'larına test push'u gönderir ve Expo'nun HAM
    // cevabını doğrudan response'ta döndürür — sunucu loguna bakmaya gerek kalmaz.
    // "status":"error" görürsen details.error alanı gerçek sebebi söyler (en sık
    // "DeviceNotRegistered" = token geçersiz/eski, "InvalidCredentials" = Expo/EAS
    // projesine APNs key hiç yüklenmemiş — iOS'ta push'un hiç düşmemesinin en yaygın
    // sebebi budur).
    [HttpPost("test-push")]
    public async Task<IActionResult> TestPush([FromServices] IHttpClientFactory httpClientFactory)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var tokens = await db.DeviceTokens
            .Where(t => t.UserId == currentUser.UserId.Value)
            .ToListAsync();

        if (tokens.Count == 0)
            return BadRequest(new { message = "Bu kullanıcıya ait kayıtlı cihaz token'ı yok — önce uygulamayı açıp push izni vermen lazım." });

        var client = httpClientFactory.CreateClient();
        var results = new List<object>();

        foreach (var token in tokens)
        {
            var response = await client.PostAsJsonAsync("https://exp.host/--/api/v2/push/send", new
            {
                to = token.ExpoPushToken,
                title = "Test bildirimi",
                body = "Bu bir test push'u — görüyorsan sistem çalışıyor demektir."
            });

            var rawBody = await response.Content.ReadAsStringAsync();

            results.Add(new
            {
                token.Platform,
                tokenPreview = token.ExpoPushToken.Length > 20 ? token.ExpoPushToken[..20] + "..." : token.ExpoPushToken,
                httpStatus = (int)response.StatusCode,
                expoRawResponse = rawBody
            });
        }

        return Ok(results);
    }

    // Logout'ta çağrılır — kullanıcıya ait o token'ı siler.
    [HttpDelete]
    public async Task<IActionResult> Remove([FromBody] RemoveDeviceTokenRequest request)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var token = await db.DeviceTokens
            .FirstOrDefaultAsync(t => t.UserId == currentUser.UserId.Value && t.ExpoPushToken == request.ExpoPushToken);

        if (token == null)
            return NotFound();

        db.DeviceTokens.Remove(token);
        await db.SaveChangesAsync();

        return NoContent();
    }
}