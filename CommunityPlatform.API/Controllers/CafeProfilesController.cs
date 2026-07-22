using CommunityPlatform.Application.DTOs.Cafe;
using CommunityPlatform.Application.DTOs.Media;
using CommunityPlatform.Application.DTOs.Reviews;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Infrastructure.Persistence;
using CommunityPlatform.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityPlatform.API.Controllers;

[ApiController]
[Route("api/v1/cafe-profiles")]
public class CafeProfilesController(
    AppDbContext db,
    ICurrentUserService currentUser,
    IStorageProvider storage,
    SafeUploadService uploads) : ControllerBase
{
    [HttpGet("dashboard")]
    [Authorize(Policy = "RequireCafeRole")]
    public async Task<IActionResult> GetDashboard()
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var profile = await db.CafeProfiles
            .Include(p => p.CafeProfileCategories)
            .Include(p => p.SpaceListings)
            .FirstOrDefaultAsync(p => p.UserId == currentUser.UserId);

        if (profile == null)
            return NotFound();

        return Ok(new CafeDashboardResponse
        {
            Name = profile.Name,
            TotalListings = profile.SpaceListings.Count,
            ActiveListings = profile.SpaceListings.Count(l => l.IsActive),
            CategoryCount = profile.CafeProfileCategories.Count
        });
    }
    [HttpGet("me")]
    [Authorize(Policy = "RequireCafeRole")]
    public async Task<IActionResult> GetMyProfile()
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var profile = await db.CafeProfiles
            .Include(p => p.User)
            .Include(p => p.City)
            .Include(p => p.District)
            .Include(p => p.CafeProfileCategories)
                .ThenInclude(cc => cc.Category)
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
            .Include(p => p.City)
            .Include(p => p.District)
            .Include(p => p.CafeProfileCategories)
            .FirstOrDefaultAsync(p => p.UserId == currentUser.UserId);

        if (profile == null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(request.Name))
            profile.Name = request.Name;

        if (request.Bio != null)
            profile.Bio = request.Bio;

        if (request.CityId != null)
            profile.CityId = request.CityId;

        if (request.DistrictId != null)
            profile.DistrictId = request.DistrictId;

        if (request.Latitude != null)
            profile.Latitude = request.Latitude;

        if (request.Longitude != null)
            profile.Longitude = request.Longitude;

        if (request.Address != null)
            profile.Address = request.Address;

        if (request.CategoryIds != null)
        {
            profile.CafeProfileCategories.Clear();
            foreach (var categoryId in request.CategoryIds.Distinct())
            {
                profile.CafeProfileCategories.Add(new CafeProfileCategory
                {
                    CafeProfileId = profile.Id,
                    CategoryId = categoryId
                });
            }
        }

        profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var updated = await db.CafeProfiles
            .Include(p => p.User)
            .Include(p => p.City)
            .Include(p => p.District)
            .Include(p => p.CafeProfileCategories)
                .ThenInclude(cc => cc.Category)
            .FirstAsync(p => p.UserId == currentUser.UserId);

        return Ok(MapToResponse(updated));
    }

    [HttpPost("me/avatar")]
    [Authorize(Policy = "RequireCafeRole")]
    [RequestSizeLimit(10_485_760)]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var profile = await db.CafeProfiles.FirstOrDefaultAsync(p => p.UserId == currentUser.UserId);
        if (profile == null)
            return NotFound();

        var oldKey = storage.TryGetKeyFromUrl(profile.AvatarUrl);
        await using var stream = file.OpenReadStream();
        var saved = await uploads.SaveImageAsync($"cafes/{profile.UserId}/avatar", stream, file.Length);

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

        var profile = await db.CafeProfiles.FirstOrDefaultAsync(p => p.UserId == currentUser.UserId);
        if (profile == null)
            return NotFound();

        var oldKey = storage.TryGetKeyFromUrl(profile.CoverImageUrl);
        await using var stream = file.OpenReadStream();
        var saved = await uploads.SaveImageAsync($"cafes/{profile.UserId}/cover", stream, file.Length);

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
            .Include(p => p.City)
            .Include(p => p.District)
            .Include(p => p.CafeProfileCategories)
                .ThenInclude(cc => cc.Category)
            .FirstOrDefaultAsync(p => p.UserId == id);

        if (profile == null)
            return NotFound();

        return Ok(MapToResponse(profile));
    }

    // Herkes bir cafe'nin yorumlarını görebilir (gizli olanlar hariç)
    [HttpGet("{id}/reviews")]
    [AllowAnonymous]
    public async Task<IActionResult> GetReviews(Guid id, [FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        var cafeProfile = await db.CafeProfiles.FirstOrDefaultAsync(p => p.UserId == id);
        if (cafeProfile == null)
            return NotFound(new { message = "Cafe profili bulunamadı." });

        var reviews = await db.Reviews
            .Include(r => r.User)
            .Where(r => r.SpaceBooking!.SpaceListing.CafeProfileId == cafeProfile.Id && r.IsVisible)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(r => new CafeReviewResponse
            {
                Id = r.Id,
                SpaceBookingId = r.SpaceBookingId!.Value,
                UserId = r.UserId,
                UserName = r.User.FirstName + " " + r.User.LastName,
                Rating = r.Rating,
                Comment = r.Comment,
                EmployerReply = r.EmployerReply,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return Ok(reviews);
    }

    private static CafeProfileResponse MapToResponse(CafeProfile profile) => new()
    {
        UserId = profile.UserId,
        Name = profile.Name,
        Bio = profile.Bio,
        City = profile.City?.Name,
        CityId = profile.CityId,
        District = profile.District?.Name,
        DistrictId = profile.DistrictId,
        Address = profile.Address,
        Latitude = profile.Latitude,
        Longitude = profile.Longitude,
        AvatarUrl = profile.AvatarUrl,
        CoverImageUrl = profile.CoverImageUrl
        ,
        CategoryIds = profile.CafeProfileCategories?.Select(cc => cc.CategoryId).ToList() ?? new List<Guid>(),
        CategoryNames = profile.CafeProfileCategories?.Select(cc => cc.Category.Name).ToList() ?? new List<string>()
    };
}
