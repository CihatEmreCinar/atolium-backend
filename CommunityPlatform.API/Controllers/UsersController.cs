using CommunityPlatform.Application.DTOs.Users;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityPlatform.API.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public class UsersController(AppDbContext db, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var user = await db.Users
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
            : await db.Enrollments.CountAsync(e => e.UserId == user.Id && e.Status == "attended");

        return Ok(new MyProfileResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            City = user.City,
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