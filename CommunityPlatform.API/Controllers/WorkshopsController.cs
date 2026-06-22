using CommunityPlatform.Application.DTOs.Workshops;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityPlatform.API.Controllers;

[ApiController]
[Route("api/v1/workshops")]
public class WorkshopsController(AppDbContext db, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        var query = db.Workshops
            .Include(w => w.Employer)
            .Include(w => w.WorkshopCategories)
                .ThenInclude(wc => wc.Category)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(w => w.Status == status);
        else
            query = query.Where(w => w.Status == "published");

        var workshops = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        return Ok(workshops.Select(MapToResponse));
    }

    [HttpGet("mine")]
    [Authorize(Roles = "employer")]
    public async Task<IActionResult> GetMine()
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var workshops = await db.Workshops
            .Include(w => w.Employer)
            .Include(w => w.WorkshopCategories)
                .ThenInclude(wc => wc.Category)
            .Where(w => w.EmployerId == currentUser.UserId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();

        return Ok(workshops.Select(MapToResponse));
    }

    [HttpGet("recommended")]
    [Authorize(Roles = "employee")]
    public async Task<IActionResult> GetRecommended()
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var profile = await db.EmployeeProfiles.FirstOrDefaultAsync(p => p.UserId == currentUser.UserId);
        var interests = profile?.Interests.Concat(profile.Hobbies).ToList() ?? [];

        var workshops = await db.Workshops
            .Include(w => w.Employer)
            .Include(w => w.WorkshopCategories)
                .ThenInclude(wc => wc.Category)
            .Where(w => w.Status == "published")
            .ToListAsync();

        var ranked = workshops
            .OrderByDescending(w => w.Tags.Intersect(interests, StringComparer.OrdinalIgnoreCase).Count())
            .ThenByDescending(w => w.CreatedAt)
            .Take(20)
            .Select(MapToResponse)
            .ToList();

        return Ok(ranked);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var workshop = await db.Workshops
            .Include(w => w.Employer)
            .Include(w => w.WorkshopCategories)
                .ThenInclude(wc => wc.Category)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (workshop == null)
            return NotFound();

        return Ok(MapToResponse(workshop));
    }

    [HttpPost]
    [Authorize(Roles = "employer")]
    public async Task<IActionResult> Create([FromBody] WorkshopRequest request)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        if (request.EndAt <= request.StartAt)
            return BadRequest(new { message = "Bitiş zamanı başlangıçtan sonra olmalı." });

        var workshop = new Workshop
        {
            EmployerId = currentUser.UserId.Value,
            Title = request.Title,
            Description = request.Description,
            CoverImageUrl = request.CoverImageUrl,
            Price = request.Price,
            Capacity = request.Capacity,
            LocationType = request.LocationType,
            LocationDetail = request.LocationDetail,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            Tags = request.Tags ?? [],
            Status = "draft"
        };

        if (request.CategoryIds != null)
        {
            foreach (var categoryId in request.CategoryIds.Distinct())
            {
                workshop.WorkshopCategories.Add(new WorkshopCategory
                {
                    WorkshopId = workshop.Id,
                    CategoryId = categoryId
                });
            }
        }

        db.Workshops.Add(workshop);
        await db.SaveChangesAsync();

        var created = await db.Workshops
            .Include(w => w.Employer)
            .Include(w => w.WorkshopCategories)
                .ThenInclude(wc => wc.Category)
            .FirstAsync(w => w.Id == workshop.Id);
        return CreatedAtAction(nameof(GetById), new { id = workshop.Id }, MapToResponse(created));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "employer")]
    public async Task<IActionResult> Update(Guid id, [FromBody] WorkshopRequest request)
    {
        var workshop = await db.Workshops
            .Include(w => w.WorkshopCategories)
            .FirstOrDefaultAsync(w => w.Id == id);
        if (workshop == null)
            return NotFound();

        if (workshop.EmployerId != currentUser.UserId)
            return Forbid();

        workshop.Title = request.Title;
        workshop.Description = request.Description;
        workshop.CoverImageUrl = request.CoverImageUrl;
        workshop.Price = request.Price;
        workshop.Capacity = request.Capacity;
        workshop.LocationType = request.LocationType;
        workshop.LocationDetail = request.LocationDetail;
        workshop.StartAt = request.StartAt;
        workshop.EndAt = request.EndAt;
        workshop.Tags = request.Tags ?? [];

        workshop.WorkshopCategories.Clear();
        if (request.CategoryIds != null)
        {
            foreach (var categoryId in request.CategoryIds.Distinct())
            {
                workshop.WorkshopCategories.Add(new WorkshopCategory
                {
                    WorkshopId = workshop.Id,
                    CategoryId = categoryId
                });
            }
        }

        await db.SaveChangesAsync();

        var updated = await db.Workshops
            .Include(w => w.Employer)
            .Include(w => w.WorkshopCategories)
                .ThenInclude(wc => wc.Category)
            .FirstAsync(w => w.Id == id);
        return Ok(MapToResponse(updated));
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "employer")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeStatusRequest request)
    {
        var workshop = await db.Workshops.FirstOrDefaultAsync(w => w.Id == id);
        if (workshop == null)
            return NotFound();

        if (workshop.EmployerId != currentUser.UserId)
            return Forbid();

        var allowedStatuses = new[] { "draft", "published", "cancelled", "completed" };
        if (!allowedStatuses.Contains(request.Status))
            return BadRequest(new { message = "Geçersiz durum." });

        if (request.Status == "published")
        {
            var profile = await db.EmployerProfiles.FirstOrDefaultAsync(p => p.UserId == currentUser.UserId);
            if (profile == null || string.IsNullOrWhiteSpace(profile.WorkshopTitle))
                return BadRequest(new { message = "Profili tamamlamadan atölye yayınlayamazsınız." });
        }

        workshop.Status = request.Status;
        await db.SaveChangesAsync();

        return Ok(new { id = workshop.Id, status = workshop.Status });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "employer")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var workshop = await db.Workshops.FirstOrDefaultAsync(w => w.Id == id);
        if (workshop == null)
            return NotFound();

        if (workshop.EmployerId != currentUser.UserId)
            return Forbid();

        workshop.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{id}/enrollments")]
    [Authorize(Roles = "employer")]
    public async Task<IActionResult> GetEnrollments(Guid id)
    {
        var workshop = await db.Workshops.FirstOrDefaultAsync(w => w.Id == id);
        if (workshop == null)
            return NotFound();

        if (workshop.EmployerId != currentUser.UserId)
            return Forbid();

        var enrollments = await db.Enrollments
            .Include(e => e.User)
            .Where(e => e.WorkshopId == id)
            .Select(e => new
            {
                e.Id,
                e.UserId,
                UserName = $"{e.User.FirstName} {e.User.LastName}",
                e.Status,
                e.TicketCode,
                e.EnrolledAt,
                e.AttendedAt
            })
            .ToListAsync();

        return Ok(enrollments);
    }

    [HttpPatch("{id}/enrollments/{eid}/attend")]
    [Authorize(Roles = "employer")]
    public async Task<IActionResult> ConfirmAttendance(Guid id, Guid eid)
    {
        var workshop = await db.Workshops.FirstOrDefaultAsync(w => w.Id == id);
        if (workshop == null)
            return NotFound();

        if (workshop.EmployerId != currentUser.UserId)
            return Forbid();

        var enrollment = await db.Enrollments.FirstOrDefaultAsync(e => e.Id == eid && e.WorkshopId == id);
        if (enrollment == null)
            return NotFound(new { message = "Kayıt bulunamadı." });

        enrollment.Status = "attended";
        enrollment.AttendedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Ok(new { id = enrollment.Id, status = enrollment.Status, attendedAt = enrollment.AttendedAt });
    }

    private static WorkshopResponse MapToResponse(Workshop w) => new()
    {
        Id = w.Id,
        EmployerId = w.EmployerId,
        EmployerName = $"{w.Employer.FirstName} {w.Employer.LastName}",
        Title = w.Title,
        Description = w.Description,
        CoverImageUrl = w.CoverImageUrl,
        Price = w.Price,
        Capacity = w.Capacity,
        EnrolledCount = w.EnrolledCount,
        LocationType = w.LocationType,
        LocationDetail = w.LocationDetail,
        StartAt = w.StartAt,
        EndAt = w.EndAt,
        Status = w.Status,
        Tags = w.Tags,
        CategoryIds = w.WorkshopCategories.Select(wc => wc.CategoryId).ToList(),
        CategoryNames = w.WorkshopCategories.Select(wc => wc.Category.Name).ToList(),
        AvgRating = w.AvgRating,
        ReviewCount = w.ReviewCount,
        CreatedAt = w.CreatedAt
    };
}

public class ChangeStatusRequest
{
    public string Status { get; set; } = null!;
}