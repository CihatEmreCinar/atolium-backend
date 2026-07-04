using CommunityPlatform.Application.DTOs.Cafe;
using CommunityPlatform.Application.DTOs.Media;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityPlatform.API.Controllers;

[ApiController]
[Route("api/v1/cafe-profiles")]
public class CafeProfilesController(
    AppDbContext db,
    ICurrentUserService currentUser,
    IStorageProvider storage) : ControllerBase
{
    [HttpGet("me")]
    [Authorize(Policy = "RequireCafeRole")]
    public async Task<IActionResult> GetMyProfile()
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var profile = await db.CafeProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == currentUser.UserId);

        if (profile == null)
            return NotFound();

        return Ok(MapToResponse(profile));
    }

    [HttpPut("me")]
    [Authorize(Policy = "RequireCafeRole")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateCafeProfileRequest request)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var profile = await db.CafeProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == currentUser.UserId);

        if (profile == null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(request.Name))
            profile.Name = request.Name;

        if (request.Bio != null)
            profile.Bio = request.Bio;

        if (request.City != null)
            profile.City = request.City;

        if (request.Address != null)
            profile.Address = request.Address;

        profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(MapToResponse(profile));
    }

    [HttpPost("me/avatar")]
    [Authorize(Policy = "RequireCafeRole")]
    [RequestSizeLimit(10_485_760)]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        if (file.Length == 0)
            return BadRequest(new { message = "Dosya boş olamaz." });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Sadece JPEG, PNG veya WEBP yükleyebilirsiniz." });

        var profile = await db.CafeProfiles.FirstOrDefaultAsync(p => p.UserId == currentUser.UserId);
        if (profile == null)
            return NotFound();

        var oldKey = storage.TryGetKeyFromUrl(profile.AvatarUrl);
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var key = $"cafes/{profile.UserId}/avatar/{Guid.NewGuid()}{extension}";

        await using var stream = file.OpenReadStream();
        var saved = await storage.SaveAsync(key, stream, file.ContentType);

        profile.AvatarUrl = saved.Url;
        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(oldKey) && !string.Equals(oldKey, saved.Key, StringComparison.OrdinalIgnoreCase))
            await storage.DeleteAsync(oldKey);

        return Ok(new FileUploadResponse { Url = saved.Url, SizeBytes = saved.SizeBytes });
    }

    [HttpPost("me/cover")]
    [Authorize(Policy = "RequireCafeRole")]
    [RequestSizeLimit(10_485_760)]
    public async Task<IActionResult> UploadCover(IFormFile file)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        if (file.Length == 0)
            return BadRequest(new { message = "Dosya boş olamaz." });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Sadece JPEG, PNG veya WEBP yükleyebilirsiniz." });

        var profile = await db.CafeProfiles.FirstOrDefaultAsync(p => p.UserId == currentUser.UserId);
        if (profile == null)
            return NotFound();

        var oldKey = storage.TryGetKeyFromUrl(profile.CoverImageUrl);
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var key = $"cafes/{profile.UserId}/cover/{Guid.NewGuid()}{extension}";

        await using var stream = file.OpenReadStream();
        var saved = await storage.SaveAsync(key, stream, file.ContentType);

        profile.CoverImageUrl = saved.Url;
        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(oldKey) && !string.Equals(oldKey, saved.Key, StringComparison.OrdinalIgnoreCase))
            await storage.DeleteAsync(oldKey);

        return Ok(new FileUploadResponse { Url = saved.Url, SizeBytes = saved.SizeBytes });
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicProfile(Guid id)
    {
        var profile = await db.CafeProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == id);

        if (profile == null)
            return NotFound();

        return Ok(MapToResponse(profile));
    }

    private static CafeProfileResponse MapToResponse(CafeProfile profile) => new()
    {
        UserId = profile.UserId,
        Name = profile.Name,
        Bio = profile.Bio,
        City = profile.City,
        Address = profile.Address,
        AvatarUrl = profile.AvatarUrl,
        CoverImageUrl = profile.CoverImageUrl
    };
}
