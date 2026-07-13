using CommunityPlatform.Application.DTOs.Employee;
using CommunityPlatform.Application.DTOs.Media;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Domain.Enums;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityPlatform.API.Controllers;

[ApiController]
[Route("api/v1/employee")]
[Authorize(Roles = "employee")]
public class EmployeeController(AppDbContext db, ICurrentUserService currentUser, IStorageProvider storage) : ControllerBase
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

        var profile = await db.EmployeeProfiles.FirstOrDefaultAsync(p => p.UserId == currentUser.UserId);
        if (profile == null)
            return NotFound();

        var oldKey = storage.TryGetKeyFromUrl(profile.CoverImageUrl);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var key = $"employees/{profile.UserId}/cover/{Guid.NewGuid()}{extension}";

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

        var profile = await db.EmployeeProfiles.FirstOrDefaultAsync(p => p.UserId == currentUser.UserId);
        if (profile == null)
            return NotFound();

        var user = await db.Users.FirstAsync(u => u.Id == currentUser.UserId);

        var attendedCount = await db.Enrollments
            .CountAsync(e => e.UserId == currentUser.UserId && e.AttendanceStatus == AttendanceStatus.Attended);

        return Ok(new EmployeeProfileResponse
        {
            UserId = profile.UserId,
            Interests = profile.Interests,
            Hobbies = profile.Hobbies,
            Bio = user.Bio,
            AvatarUrl = user.AvatarUrl,
            CoverImageUrl = profile.CoverImageUrl,
            City = user.City,
            TotalAttendedWorkshops = attendedCount,
            XpPoints = user.XpPoints,
            RankLevel = user.RankLevel
        });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] EmployeeProfileRequest request)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var profile = await db.EmployeeProfiles.FirstOrDefaultAsync(p => p.UserId == currentUser.UserId);
        if (profile == null)
            return NotFound();

        profile.Interests = request.Interests ?? [];
        profile.Hobbies = request.Hobbies ?? [];

        var user = await db.Users.FirstAsync(u => u.Id == currentUser.UserId);
        user.Bio = request.Bio;
        if (request.AvatarUrl != null)
            user.AvatarUrl = request.AvatarUrl;
        if (request.CoverImageUrl != null)
            profile.CoverImageUrl = request.CoverImageUrl;
        user.City = request.City;

        await db.SaveChangesAsync();

        var attendedCount = await db.Enrollments
            .CountAsync(e => e.UserId == currentUser.UserId && e.AttendanceStatus == AttendanceStatus.Attended);

        return Ok(new EmployeeProfileResponse
        {
            UserId = profile.UserId,
            Interests = profile.Interests,
            Hobbies = profile.Hobbies,
            Bio = user.Bio,
            AvatarUrl = user.AvatarUrl,
            CoverImageUrl = profile.CoverImageUrl,
            City = user.City,
            TotalAttendedWorkshops = attendedCount,
            XpPoints = user.XpPoints,
            RankLevel = user.RankLevel
        });
    }
}