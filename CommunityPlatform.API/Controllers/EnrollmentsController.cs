using CommunityPlatform.Application.DTOs.Enrollments;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityPlatform.API.Controllers;

[ApiController]
[Route("api/v1/enrollments")]
[Authorize]
public class EnrollmentsController(
    AppDbContext db,
    ICurrentUserService currentUser,
    INotificationService notifications) : ControllerBase
{
    // Employee atölyeye kayıt olur
    [HttpPost]
    [Authorize(Roles = "employee")]
    public async Task<IActionResult> Create([FromBody] EnrollmentRequest request)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var workshop = await db.Workshops.FirstOrDefaultAsync(w => w.Id == request.WorkshopId);
        if (workshop == null)
            return NotFound(new { message = "Atölye bulunamadı." });

        if (workshop.Status != "published")
            return BadRequest(new { message = "Bu atölye şu anda kayıt almıyor." });

        var existingEnrollment = await db.Enrollments
            .FirstOrDefaultAsync(e => e.WorkshopId == workshop.Id && e.UserId == currentUser.UserId);

        if (existingEnrollment != null && existingEnrollment.Status != "cancelled")
            return Conflict(new { message = "Bu atölyeye zaten kayıtlısınız." });

        if (workshop.EnrolledCount >= workshop.Capacity)
            return BadRequest(new { message = "Atölye kapasitesi dolu." });

        Enrollment enrollment;

        if (existingEnrollment != null)
        {
            existingEnrollment.Status = "confirmed";
            existingEnrollment.EnrolledAt = DateTime.UtcNow;
            existingEnrollment.AttendedAt = null;
            existingEnrollment.TicketCode = Guid.NewGuid().ToString("N");
            enrollment = existingEnrollment;
        }
        else
        {
            enrollment = new Enrollment
            {
                WorkshopId = workshop.Id,
                UserId = currentUser.UserId.Value,
                Status = "confirmed"
            };
            db.Enrollments.Add(enrollment);
        }

        workshop.EnrolledCount += 1;
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = enrollment.Id }, MapToResponse(enrollment, workshop));
    }

    // Kendi kayıtlarım
    [HttpGet("me")]
    [Authorize(Roles = "employee")]
    public async Task<IActionResult> GetMine()
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var enrollments = await db.Enrollments
            .Include(e => e.Workshop)
            .Where(e => e.UserId == currentUser.UserId)
            .OrderByDescending(e => e.EnrolledAt)
            .Select(e => MapToResponse(e, e.Workshop))
            .ToListAsync();

        return Ok(enrollments);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var enrollment = await db.Enrollments
            .Include(e => e.Workshop)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (enrollment == null)
            return NotFound();

        if (enrollment.UserId != currentUser.UserId)
            return Forbid();

        return Ok(MapToResponse(enrollment, enrollment.Workshop));
    }

    // Kayıt iptali
    [HttpDelete("{id}")]
    [Authorize(Roles = "employee")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var enrollment = await db.Enrollments
            .Include(e => e.Workshop)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (enrollment == null)
            return NotFound();

        if (enrollment.UserId != currentUser.UserId)
            return Forbid();

        if (enrollment.Status == "cancelled")
            return BadRequest(new { message = "Kayıt zaten iptal edilmiş." });

        enrollment.Status = "cancelled";
        enrollment.Workshop.EnrolledCount = Math.Max(0, enrollment.Workshop.EnrolledCount - 1);
        await db.SaveChangesAsync();

        return NoContent();
    }

    // Employer katılımı teyit eder → employee'ye in-app + email bildirim
    [HttpPatch("{id}/attend")]
    [Authorize(Roles = "employer")]
    public async Task<IActionResult> MarkAttended(Guid id)
    {
        var enrollment = await db.Enrollments
            .Include(e => e.Workshop)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (enrollment == null)
            return NotFound();

        if (enrollment.Workshop.EmployerId != currentUser.UserId)
            return Forbid();

        if (enrollment.Status != "confirmed")
            return BadRequest(new { message = "Sadece confirmed kayıtlar attended yapılabilir." });

        enrollment.Status = "attended";
        enrollment.AttendedAt = DateTime.UtcNow;

        // XP: katılım +25
        var employee = await db.Users.FirstAsync(u => u.Id == enrollment.UserId);
        employee.XpPoints += 25;

        await db.SaveChangesAsync();

        // In-app + email bildirim
        await notifications.NotifyAsync(
            userId: enrollment.UserId,
            type: NotificationType.WorkshopCompleted,
            title: "Katılımınız teyit edildi!",
            body: $"{enrollment.Workshop.Title} atölyesine katılımınız onaylandı. +25 XP kazandınız!",
            metadata: new { workshopId = enrollment.WorkshopId, route = "workshop/detail" },
            sendEmail: true);

        return Ok(new
        {
            id = enrollment.Id,
            status = enrollment.Status,
            attendedAt = enrollment.AttendedAt
        });
    }

    private static EnrollmentResponse MapToResponse(Enrollment e, Workshop w) => new()
    {
        Id = e.Id,
        WorkshopId = w.Id,
        WorkshopTitle = w.Title,
        WorkshopStartAt = w.StartAt,
        Status = e.Status,
        TicketCode = e.TicketCode,
        EnrolledAt = e.EnrolledAt,
        AttendedAt = e.AttendedAt
    };
}
