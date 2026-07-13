using CommunityPlatform.Application.DTOs.Employee;
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
public class EmployeeController(AppDbContext db, ICurrentUserService currentUser) : ControllerBase
{
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
            City = user.City,
            TotalAttendedWorkshops = attendedCount,
            XpPoints = user.XpPoints,
            RankLevel = user.RankLevel
        });
    }
}