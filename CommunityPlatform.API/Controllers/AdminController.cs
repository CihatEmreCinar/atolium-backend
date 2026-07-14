using CommunityPlatform.Application.DTOs.Admin;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityPlatform.API.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "admin")]
public class AdminController(AppDbContext db) : ControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? role, [FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        if (page < 1) page = 1;
        if (limit is < 1 or > 100) limit = 20;

        var query = db.Users.AsQueryable();

        if (!string.IsNullOrEmpty(role))
            query = query.Where(u => u.Role == role);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(u => new UserListItem
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost("users/{id}/promote-to-admin")]
    public async Task<IActionResult> PromoteToAdmin(Guid id)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        user.Role = "admin";
        await db.SaveChangesAsync();
        return Ok(new { id = user.Id, role = user.Role });
    }

    [HttpPatch("users/{id}/status")]
    public async Task<IActionResult> UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return NotFound();

        user.IsActive = request.IsActive;

        // Banlanan kullanıcının elindeki tüm refresh token'ları iptal et.
        // Aksi halde login engellense de refresh ile oturumunu süresiz yenileyebilir.
        if (!request.IsActive)
        {
            var activeTokens = await db.RefreshTokens
                .Where(rt => rt.UserId == id && !rt.IsRevoked)
                .ToListAsync();

            foreach (var token in activeTokens)
                token.IsRevoked = true;
        }

        await db.SaveChangesAsync();

        return Ok(new { id = user.Id, isActive = user.IsActive });
    }

    [HttpGet("workshops")]
    public async Task<IActionResult> GetWorkshopsForModeration([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        if (page < 1) page = 1;
        if (limit is < 1 or > 100) limit = 20;

        var query = db.Workshops.Include(w => w.Employer).AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(w => w.Status == status);

        var workshops = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(w => new WorkshopModerationItem
            {
                Id = w.Id,
                Title = w.Title,
                EmployerName = $"{w.Employer.FirstName} {w.Employer.LastName}",
                Status = w.Status,
                CreatedAt = w.CreatedAt
            })
            .ToListAsync();

        return Ok(workshops);
    }

    [HttpPatch("workshops/{id}/approve")]
    public async Task<IActionResult> ApproveWorkshop(Guid id)
    {
        var workshop = await db.Workshops.FirstOrDefaultAsync(w => w.Id == id);
        if (workshop == null)
            return NotFound();

        workshop.Status = "published";
        await db.SaveChangesAsync();

        return Ok(new { id = workshop.Id, status = workshop.Status });
    }

    [HttpPatch("workshops/{id}/reject")]
    public async Task<IActionResult> RejectWorkshop(Guid id, [FromBody] RejectWorkshopRequest request)
    {
        var workshop = await db.Workshops.FirstOrDefaultAsync(w => w.Id == id);
        if (workshop == null)
            return NotFound();

        workshop.Status = "cancelled";
        await db.SaveChangesAsync();

        return Ok(new { id = workshop.Id, status = workshop.Status, reason = request.Reason });
    }

    [HttpGet("analytics/overview")]
    public async Task<IActionResult> GetOverview()
    {
        var totalUsers = await db.Users.CountAsync();
        var totalEmployers = await db.Users.CountAsync(u => u.Role == "employer");
        var totalEmployees = await db.Users.CountAsync(u => u.Role == "employee");
        var totalWorkshops = await db.Workshops.CountAsync();
        var publishedWorkshops = await db.Workshops.CountAsync(w => w.Status == "published");
        var totalEnrollments = await db.Enrollments.CountAsync(e => e.Status != "cancelled");
        var totalReviews = await db.Reviews.CountAsync();

        return Ok(new
        {
            totalUsers,
            totalEmployers,
            totalEmployees,
            totalWorkshops,
            publishedWorkshops,
            totalEnrollments,
            totalReviews
        });
    }
}