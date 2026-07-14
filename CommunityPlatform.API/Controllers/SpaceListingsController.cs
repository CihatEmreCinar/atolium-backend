using CommunityPlatform.Application.DTOs.Media;
using CommunityPlatform.Application.DTOs.SpaceListings;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityPlatform.API.Controllers;

[ApiController]
[Route("api/v1/space-listings")]
public class SpaceListingsController(
    AppDbContext db,
    ICurrentUserService currentUser,
    IStorageProvider storage) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "RequireCafeRole")]
    public async Task<IActionResult> Create([FromBody] CreateSpaceListingRequest request)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "Başlık zorunludur." });

        if (request.Capacity < 1)
            return BadRequest(new { message = "Kapasite en az 1 olmalı." });

        if (request.HourlyPrice < 0)
            return BadRequest(new { message = "Saatlik ücret negatif olamaz." });

        // Important: resolve the current cafe profile by UserId first, then use CafeProfile.Id for the listing.
        var cafeProfile = await db.CafeProfiles.FirstOrDefaultAsync(p => p.UserId == currentUser.UserId.Value);
        if (cafeProfile == null)
            return NotFound(new { message = "Cafe profili bulunamadı." });

        var listing = new SpaceListing
        {
            CafeProfileId = cafeProfile.Id,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Capacity = request.Capacity,
            HourlyPrice = request.HourlyPrice,
            Amenities = request.Amenities?.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? [],
            IsActive = true
        };

        db.SpaceListings.Add(listing);
        await db.SaveChangesAsync();

        var created = await db.SpaceListings
            .Include(l => l.CafeProfile).ThenInclude(c => c!.City)
            .Include(l => l.Photos)
            .FirstAsync(l => l.Id == listing.Id);

        return CreatedAtAction(nameof(GetById), new { id = listing.Id }, MapToResponse(created));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "RequireCafeRole")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSpaceListingRequest request)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var listing = await db.SpaceListings
            .Include(l => l.CafeProfile).ThenInclude(c => c!.City)
            .Include(l => l.Photos)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (listing == null)
            return NotFound();

        if (listing.CafeProfile.UserId != currentUser.UserId.Value)
            return Forbid();

        if (!string.IsNullOrWhiteSpace(request.Title))
            listing.Title = request.Title.Trim();

        if (request.Description != null)
            listing.Description = request.Description.Trim();

        if (request.Capacity.HasValue)
            listing.Capacity = request.Capacity.Value;

        if (request.HourlyPrice.HasValue)
            listing.HourlyPrice = request.HourlyPrice.Value;

        if (request.Amenities != null)
            listing.Amenities = request.Amenities.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        if (request.IsActive.HasValue)
            listing.IsActive = request.IsActive.Value;

        listing.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(MapToResponse(listing));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireCafeRole")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var listing = await db.SpaceListings
            .Include(l => l.CafeProfile).ThenInclude(c => c!.City)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (listing == null)
            return NotFound();

        if (listing.CafeProfile.UserId != currentUser.UserId.Value)
            return Forbid();

        listing.IsActive = false;
        listing.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("mine")]
    [Authorize(Policy = "RequireCafeRole")]
    public async Task<IActionResult> GetMine()
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var cafeProfile = await db.CafeProfiles.FirstOrDefaultAsync(p => p.UserId == currentUser.UserId.Value);
        if (cafeProfile == null)
            return NotFound(new { message = "Cafe profili bulunamadı." });

        var listings = await db.SpaceListings
            .Include(l => l.CafeProfile).ThenInclude(c => c!.City)
            .Include(l => l.Photos)
            .Where(l => l.CafeProfileId == cafeProfile.Id)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        return Ok(listings.Select(MapToResponse));
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var listing = await db.SpaceListings
            .Include(l => l.CafeProfile).ThenInclude(c => c!.City)
            .Include(l => l.Photos)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (listing == null || !listing.IsActive)
            return NotFound();

        return Ok(MapToResponse(listing));
    }

    [HttpPost("{id}/photos")]
    [Authorize(Policy = "RequireCafeRole")]
    [RequestSizeLimit(10_485_760)]
    public async Task<IActionResult> UploadPhoto(Guid id, IFormFile file)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Dosya boş olamaz." });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Sadece JPEG, PNG veya WEBP yükleyebilirsiniz." });

        var listing = await db.SpaceListings
            .Include(l => l.CafeProfile).ThenInclude(c => c!.City)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (listing == null)
            return NotFound();

        if (listing.CafeProfile.UserId != currentUser.UserId.Value)
            return Forbid();

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var key = $"spaces/{listing.Id}/{Guid.NewGuid()}{extension}";

        await using var stream = file.OpenReadStream();
        var saved = await storage.SaveAsync(key, stream, file.ContentType);

        // FIX: Önceden burada sadece storage'a kaydedip URL dönülüyordu — SpaceListing'e
        // hiç bağlanmadığı için upload "başarılı" dönmesine rağmen fotoğraf hiçbir
        // response'ta (GetMine/GetById/Search) görünmüyordu. Şimdi kalıcı satır olarak ekleniyor.
        var nextOrderIndex = (short)(await db.SpaceListingPhotos
            .Where(p => p.SpaceListingId == listing.Id)
            .CountAsync());

        var photo = new SpaceListingPhoto
        {
            SpaceListingId = listing.Id,
            StorageKey = saved.Key,
            Url = saved.Url,
            OrderIndex = nextOrderIndex,
            SizeBytes = saved.SizeBytes
        };

        db.SpaceListingPhotos.Add(photo);
        await db.SaveChangesAsync();

        return Ok(new FileUploadResponse { Url = saved.Url, SizeBytes = saved.SizeBytes });
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search(
        [FromQuery] string? city,
        [FromQuery] int? minCapacity,
        [FromQuery] int? maxCapacity,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = db.SpaceListings
            .Include(l => l.CafeProfile).ThenInclude(c => c!.City)
            .Include(l => l.Photos)
            .Where(l => l.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(l => l.CafeProfile.City != null && l.CafeProfile.City.Name.ToLower().Contains(city.Trim().ToLower()));

        if (minCapacity.HasValue)
            query = query.Where(l => l.Capacity >= minCapacity.Value);

        if (maxCapacity.HasValue)
            query = query.Where(l => l.Capacity <= maxCapacity.Value);

        if (minPrice.HasValue)
            query = query.Where(l => l.HourlyPrice >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(l => l.HourlyPrice <= maxPrice.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            page,
            pageSize,
            total,
            totalPages = (int)Math.Ceiling(total / (double)pageSize),
            items = items.Select(MapToResponse)
        });
    }

    private static SpaceListingResponse MapToResponse(SpaceListing listing) => new()
    {
        Id = listing.Id,
        CafeProfileId = listing.CafeProfileId,
        Title = listing.Title,
        Description = listing.Description,
        Capacity = listing.Capacity,
        HourlyPrice = listing.HourlyPrice,
        Amenities = listing.Amenities ?? [],
        PhotoUrls = listing.Photos
            .OrderBy(p => p.OrderIndex)
            .Select(p => p.Url)
            .ToList(),
        IsActive = listing.IsActive,
        CreatedAt = listing.CreatedAt,
        UpdatedAt = listing.UpdatedAt,
        CafeName = listing.CafeProfile?.Name,
        CafeCity = listing.CafeProfile?.City?.Name,
        CafeAvatarUrl = listing.CafeProfile?.AvatarUrl
    };
}
