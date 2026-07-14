using CommunityPlatform.Application.Common;
using CommunityPlatform.Application.DTOs.Employer;
using CommunityPlatform.Application.DTOs.Media;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CommunityPlatform.Domain.Entities;

namespace CommunityPlatform.API.Controllers;

[ApiController]
[Route("api/v1/employer")]
[Authorize(Policy = "RequireEmployerRole")]
public class EmployerController(
    AppDbContext db,
    ICurrentUserService currentUser,
    IStorageProvider storage) : ControllerBase
{
    [HttpPost("profile/cover")]
    [RequestSizeLimit(10_485_760)]
    public async Task<IActionResult> UploadCoverImage(IFormFile file)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        if (file.Length == 0)
            return BadRequest(new { message = "Dosya boş olamaz." });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Sadece JPEG, PNG veya WEBP yükleyebilirsiniz." });

        var profile = await db.EmployerProfiles.FirstOrDefaultAsync(p => p.UserId == currentUser.UserId);
        if (profile == null)
            return NotFound();

        var oldKey = storage.TryGetKeyFromUrl(profile.CoverImageUrl);

        if (!FileUploadValidator.TryGetSafeExtension(file.ContentType, allowVideo: false, out var extension))
            return BadRequest(new { message = "Desteklenmeyen dosya tipi." });
        var key = $"employers/{profile.UserId}/cover/{Guid.NewGuid()}{extension}";

        await using var stream = file.OpenReadStream();
        var saved = await storage.SaveAsync(key, stream, file.ContentType);

        profile.CoverImageUrl = saved.Url;
        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(oldKey) && !string.Equals(oldKey, saved.Key, StringComparison.OrdinalIgnoreCase))
            await storage.DeleteAsync(oldKey);

        return Ok(new FileUploadResponse
        {
            Url = saved.Url,
            SizeBytes = saved.SizeBytes
        });
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var profile = await db.EmployerProfiles
            .Include(p => p.User)
            .Include(p => p.EmployerProfileCategories)
                .ThenInclude(ec => ec.Category)
            .FirstOrDefaultAsync(p => p.UserId == currentUser.UserId);

        if (profile == null)
            return NotFound();

        return Ok(MapToResponse(profile));
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] EmployerProfileRequest request)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var profile = await db.EmployerProfiles
            .Include(p => p.User)
            .Include(p => p.EmployerProfileCategories)
            .FirstOrDefaultAsync(p => p.UserId == currentUser.UserId);
        if (profile == null)
            return NotFound();

        profile.WorkshopTitle = request.WorkshopTitle;
        profile.Specialization = request.Specialization ?? [];
        profile.YearsExperience = request.YearsExperience;
        if (request.CoverImageUrl != null)
            profile.CoverImageUrl = request.CoverImageUrl;
        profile.User.Bio = request.Bio;
        if (request.ProfileImageUrl != null)
            profile.User.AvatarUrl = request.ProfileImageUrl;
        profile.User.City = request.City;

        profile.EmployerProfileCategories.Clear();
        if (request.CategoryIds != null)
        {
            foreach (var categoryId in request.CategoryIds.Distinct())
            {
                profile.EmployerProfileCategories.Add(new EmployerProfileCategory
                {
                    EmployerProfileId = profile.Id,
                    CategoryId = categoryId
                });
            }
        }

        await db.SaveChangesAsync();

        var updated = await db.EmployerProfiles
            .Include(p => p.User)
            .Include(p => p.EmployerProfileCategories)
                .ThenInclude(ec => ec.Category)
            .FirstAsync(p => p.UserId == currentUser.UserId);

        return Ok(MapToResponse(updated));
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var employerId = currentUser.UserId.Value;

        var workshops = await db.Workshops
            .Where(w => w.EmployerId == employerId)
            .ToListAsync();

        var activeWorkshops = workshops.Count(w => w.Status == "published");
        var workshopIds = workshops.Select(w => w.Id).ToList();

        var totalEnrollments = await db.Enrollments
            .CountAsync(e => workshopIds.Contains(e.WorkshopId) && e.Status != "cancelled");

        var pendingEnrollments = await db.Enrollments
            .CountAsync(e => workshopIds.Contains(e.WorkshopId) && e.Status == "confirmed");

        var reviewCount = await db.Reviews
            .CountAsync(r => r.WorkshopId != null && workshopIds.Contains(r.WorkshopId.Value));

        var avgRating = workshops.Count != 0 && workshops.Any(w => w.ReviewCount > 0)
            ? workshops.Where(w => w.ReviewCount > 0).Average(w => w.AvgRating)
            : 0;

        var user = await db.Users.FirstAsync(u => u.Id == employerId);
        var profile = await db.EmployerProfiles.FirstOrDefaultAsync(p => p.UserId == employerId);

        return Ok(new EmployerDashboardResponse
        {
            ActiveWorkshops = activeWorkshops,
            TotalWorkshops = workshops.Count,
            PendingEnrollments = pendingEnrollments,
            TotalEnrollments = totalEnrollments,
            AvgRating = avgRating,
            ReviewCount = reviewCount,
            XpPoints = user.XpPoints,
            RankLevel = user.RankLevel,
            EmployerRank = profile?.EmployerRank ?? "Yeni"
        });
    }

    private static EmployerProfileResponse MapToResponse(EmployerProfile p) => new()
    {
        UserId = p.UserId,
        WorkshopTitle = p.WorkshopTitle,
        Specialization = p.Specialization,
        CategoryIds = p.EmployerProfileCategories.Select(ec => ec.CategoryId).ToList(),
        CategoryNames = p.EmployerProfileCategories.Select(ec => ec.Category.Name).ToList(),
        YearsExperience = p.YearsExperience,
        CoverImageUrl = p.CoverImageUrl,
        AvgRating = p.AvgRating,
        TotalWorkshops = p.TotalWorkshops,
        Bio = p.User.Bio,
        ProfileImageUrl = p.User.AvatarUrl,
        City = p.User.City,
        EmployerRank = p.EmployerRank
    };

    // Herkese açık: bir atölyecinin public profilini gösterir
    [HttpGet("/api/v1/employers/{id}/profile")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicProfile(Guid id)
    {
        var profile = await db.EmployerProfiles
            .Include(p => p.User)
            .Include(p => p.EmployerProfileCategories)
                .ThenInclude(ec => ec.Category)
            .FirstOrDefaultAsync(p => p.UserId == id);

        if (profile == null)
            return NotFound();

        var workshops = await db.Workshops
            .Where(w => w.EmployerId == id && w.Status == "published")
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new PublicWorkshopItem
            {
                Id = w.Id,
                Title = w.Title,
                Price = w.Price,
                AvgRating = w.AvgRating,
                StartAt = w.StartAt
            })
            .ToListAsync();

        return Ok(new EmployerPublicProfileResponse
        {
            UserId = profile.UserId,
            FirstName = profile.User.FirstName,
            LastName = profile.User.LastName,
            WorkshopTitle = profile.WorkshopTitle,
            Bio = profile.User.Bio,
            ProfileImageUrl = profile.User.AvatarUrl,
            City = profile.User.City,
            Specialization = profile.Specialization,
            CategoryNames = profile.EmployerProfileCategories.Select(ec => ec.Category.Name).ToList(),
            YearsExperience = profile.YearsExperience,
            AvgRating = profile.AvgRating,
            TotalWorkshops = profile.TotalWorkshops,
            EmployerRank = profile.EmployerRank,
            Workshops = workshops
        });
    }
}