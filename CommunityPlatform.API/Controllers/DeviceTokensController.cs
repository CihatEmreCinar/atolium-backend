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

        // ExpoPushToken bir cihaza aittir; aynı cihazda farklı bir kullanıcı
        // giriş yapmış olabilir (logout/login), bu yüzden token üzerinden
        // global arıyoruz ve bulunursa kullanıcıyı güncelliyoruz.
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
