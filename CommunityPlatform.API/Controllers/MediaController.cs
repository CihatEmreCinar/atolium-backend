using CommunityPlatform.Application.DTOs.Media;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Domain.Enums;
using CommunityPlatform.Infrastructure.Persistence;
using CommunityPlatform.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityPlatform.API.Controllers;

// Yeni unified medya pipeline'ının giriş noktası. Görevi yalnızca:
// doğrula → temp'e yükle → event yayınla. Resize/format dönüşümü YOK —
// bunlar zaten mobile'da (WEBP) ve worker'da (variant) yapılıyor.
[ApiController]
[Route("api/v1/media")]
[Authorize]
public class MediaController(
    AppDbContext db,
    IMediaObjectStore objectStore,
    IRabbitMqPublisher publisher,
    ICurrentUserService currentUser,
    SafeUploadService uploads) : ControllerBase
{
    private const string AllowedContentType = "image/webp";

    [HttpPost]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, [FromQuery] MediaPreset preset, CancellationToken ct)
    {
        if (currentUser.UserId is not { } ownerId)
            return Unauthorized();

        var media = new Media
        {
            OwnerId = ownerId,
            Preset = preset,
            Bucket = "media",
            TempObjectKey = $"temp/{Guid.NewGuid()}.webp",
            MimeType = AllowedContentType,
            FileSize = file.Length,
        };

        await using (var stream = file.OpenReadStream())
        {
            await uploads.ValidateWebpAsync(stream, file.Length, ct);
            await objectStore.PutAsync(media.TempObjectKey, stream, AllowedContentType, ct);
        }

        db.Media.Add(media);
        await db.SaveChangesAsync(ct);

        await publisher.PublishAsync("media.processing", new MediaUploadedEvent(
            media.Id, media.Bucket, media.TempObjectKey, preset.ToString()));

        return Accepted(new { mediaId = media.Id, status = media.Status.ToString() });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var media = await db.Media
            .Include(m => m.Variants)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        if (media is null) return NotFound();

        return Ok(new
        {
            id = media.Id,
            status = media.Status.ToString(),
            width = media.Width,
            height = media.Height,
            variants = media.Variants
                .OrderBy(v => v.Width)
                .Select(v => new { v.Width, v.Height, v.ObjectKey })
        });
    }
}
