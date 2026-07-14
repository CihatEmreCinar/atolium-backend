using CommunityPlatform.Application.DTOs.Media;
using CommunityPlatform.Application.DTOs.Users;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Domain.Enums;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityPlatform.API.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public class UsersController(
    AppDbContext db,
    ICurrentUserService currentUser,
    IStorageProvider storage) : ControllerBase
{
    [HttpPost("me/avatar")]
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

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId);
        if (user == null)
            return NotFound();

        var oldKey = storage.TryGetKeyFromUrl(user.AvatarUrl);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var key = $"users/{user.Id}/avatar/{Guid.NewGuid()}{extension}";

        await using var stream = file.OpenReadStream();
        var saved = await storage.SaveAsync(key, stream, file.ContentType);

        user.AvatarUrl = saved.Url;
        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(oldKey) && !string.Equals(oldKey, saved.Key, StringComparison.OrdinalIgnoreCase))
            await storage.DeleteAsync(oldKey);

        return Ok(new FileUploadResponse
        {
            Url = saved.Url,
            SizeBytes = saved.SizeBytes
        });
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var user = await db.Users
            .Include(u => u.City)
            .Include(u => u.District)
            .Include(u => u.EmployeeProfile)
            .Include(u => u.EmployerProfile)
                .ThenInclude(p => p!.EmployerProfileCategories)
                    .ThenInclude(pc => pc.Category)
            .Where(u => u.Id == currentUser.UserId)
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound();

        var attendedCount = user.EmployeeProfile == null
            ? 0
            : await db.Enrollments.CountAsync(e => e.UserId == user.Id && e.AttendanceStatus == AttendanceStatus.Attended);

        return Ok(new MyProfileResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            City = user.City?.Name,
            CityId = user.CityId,
            District = user.District?.Name,
            DistrictId = user.DistrictId,
            Bio = user.Bio,
            AvatarUrl = user.AvatarUrl,
            XpPoints = user.XpPoints,
            RankLevel = user.RankLevel,
            IsVerified = user.IsVerified,
            CreatedAt = user.CreatedAt,
            EmployeeProfile = user.EmployeeProfile == null
                ? null
                : new MyEmployeeProfileResponse
                {
                    Interests = user.EmployeeProfile.Interests,
                    Hobbies = user.EmployeeProfile.Hobbies,
                    TotalAttendedWorkshops = attendedCount
                },
            EmployerProfile = user.EmployerProfile == null
                ? null
                : new MyEmployerProfileResponse
                {
                    WorkshopTitle = user.EmployerProfile.WorkshopTitle,
                    Specialization = user.EmployerProfile.Specialization,
                    CategoryIds = user.EmployerProfile.EmployerProfileCategories.Select(pc => pc.CategoryId).ToList(),
                    CategoryNames = user.EmployerProfile.EmployerProfileCategories.Select(pc => pc.Category.Name).ToList(),
                    YearsExperience = user.EmployerProfile.YearsExperience,
                    CoverImageUrl = user.EmployerProfile.CoverImageUrl,
                    AvgRating = user.EmployerProfile.AvgRating,
                    TotalWorkshops = user.EmployerProfile.TotalWorkshops,
                    EmployerRank = user.EmployerProfile.EmployerRank
                }
        });
    }
}