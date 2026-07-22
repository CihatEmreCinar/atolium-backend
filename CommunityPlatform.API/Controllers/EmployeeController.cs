using CommunityPlatform.Application.DTOs.Employee;
using CommunityPlatform.Application.DTOs.Media;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Domain.Enums;
using CommunityPlatform.Infrastructure.Persistence;
using CommunityPlatform.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityPlatform.API.Controllers;

[ApiController]
[Route("api/v1/employee")]
[Authorize(Roles = "employee")]
public class EmployeeController(AppDbContext db, ICurrentUserService currentUser, IStorageProvider storage, SafeUploadService uploads) : ControllerBase
{
    [HttpPost("profile/cover")]
    [RequestSizeLimit(10_485_760)]
    public async Task<IActionResult> UploadCoverImage(IFormFile file)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var profile = await db.EmployeeProfiles.FirstOrDefaultAsync(p => p.UserId == currentUser.UserId);
        if (profile == null)
            return NotFound();

        var oldKey = storage.TryGetKeyFromUrl(profile.CoverImageUrl);

        await using var stream = file.OpenReadStream();
        var saved = await uploads.SaveImageAsync($"employees/{profile.UserId}/cover", stream, file.Length);

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

        var profile = await db.EmployeeProfiles
            .Include(p => p.PreferredCity)
            .Include(p => p.PreferredDistrict)
            .FirstOrDefaultAsync(p => p.UserId == currentUser.UserId);
        if (profile == null)
            return NotFound();

        var user = await db.Users
            .Include(u => u.City)
            .Include(u => u.District)
            .FirstAsync(u => u.Id == currentUser.UserId);

        var attendedCount = await db.Enrollments
            .CountAsync(e => e.UserId == currentUser.UserId && e.AttendanceStatus == AttendanceStatus.Attended);

        return Ok(MapToResponse(profile, user, attendedCount));
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
        profile.PreferredCityId = request.PreferredCityId;
        profile.PreferredDistrictId = request.PreferredDistrictId;

        var user = await db.Users.FirstAsync(u => u.Id == currentUser.UserId);
        user.Bio = request.Bio;
        if (request.AvatarUrl != null)
            user.AvatarUrl = request.AvatarUrl;
        if (request.CoverImageUrl != null)
            profile.CoverImageUrl = request.CoverImageUrl;
        user.CityId = request.CityId;
        user.DistrictId = request.DistrictId;

        await db.SaveChangesAsync();

        var updatedProfile = await db.EmployeeProfiles
            .Include(p => p.PreferredCity)
            .Include(p => p.PreferredDistrict)
            .FirstAsync(p => p.UserId == currentUser.UserId);

        var updatedUser = await db.Users
            .Include(u => u.City)
            .Include(u => u.District)
            .FirstAsync(u => u.Id == currentUser.UserId);

        var attendedCount = await db.Enrollments
            .CountAsync(e => e.UserId == currentUser.UserId && e.AttendanceStatus == AttendanceStatus.Attended);

        return Ok(MapToResponse(updatedProfile, updatedUser, attendedCount));
    }

    private static EmployeeProfileResponse MapToResponse(
        Domain.Entities.EmployeeProfile profile,
        Domain.Entities.User user,
        int attendedCount) => new()
    {
        UserId = profile.UserId,
        Interests = profile.Interests,
        Hobbies = profile.Hobbies,
        Bio = user.Bio,
        AvatarUrl = user.AvatarUrl,
        CoverImageUrl = profile.CoverImageUrl,
        City = user.City?.Name,
        CityId = user.CityId,
        District = user.District?.Name,
        DistrictId = user.DistrictId,
        PreferredCity = profile.PreferredCity?.Name,
        PreferredCityId = profile.PreferredCityId,
        PreferredDistrict = profile.PreferredDistrict?.Name,
        PreferredDistrictId = profile.PreferredDistrictId,
        TotalAttendedWorkshops = attendedCount,
        XpPoints = user.XpPoints,
        RankLevel = user.RankLevel
    };
}
