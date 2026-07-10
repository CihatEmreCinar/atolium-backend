using CommunityPlatform.Application.DTOs.Enrollments;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Domain.Enums;
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
    INotificationService notifications,
    IReminderService reminderService) : ControllerBase
{
    // Employee atölyeye kayıt başvurusu yapar → "pending"
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
            return Conflict(new { message = "Bu atölyeye zaten başvurdunuz." });

        // Sadece onaylanmış kayıtlar kapasiteyi doldurur; pending başvurular kapasiteyi hemen tüketmez
        if (workshop.EnrolledCount >= workshop.Capacity)
            return BadRequest(new { message = "Atölye kapasitesi dolu." });

        Enrollment enrollment;

        if (existingEnrollment != null)
        {
            existingEnrollment.Status = "pending";
            existingEnrollment.EnrolledAt = DateTime.UtcNow;
            existingEnrollment.AttendedAt = null;
            existingEnrollment.TicketCode = string.Empty;
            enrollment = existingEnrollment;
        }
        else
        {
            enrollment = new Enrollment
            {
                WorkshopId = workshop.Id,
                UserId = currentUser.UserId.Value,
                Status = "pending"
            };
            db.Enrollments.Add(enrollment);
        }

        await db.SaveChangesAsync();

        var applicantUser = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == currentUser.UserId!.Value)
            .Select(u => new { u.FullName })
            .FirstOrDefaultAsync();

        await notifications.NotifyAsync(
            userId:    workshop.EmployerId,
            type:      NotificationType.ApplicationReceived,
            title:     "Yeni katılım başvurusu",
            body:      $"{applicantUser?.FullName ?? "Bir kullanıcı"} \"{workshop.Title}\" atölyenize katılmak istiyor.",
            metadata:  new { enrollmentId = enrollment.Id, workshopId = workshop.Id, applicantUserId = currentUser.UserId },
            sendEmail: true);

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

    // Kayıt iptali (employee)
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

        var wasConfirmed = enrollment.Status == "confirmed";
        enrollment.Status = "cancelled";

        if (wasConfirmed)
            enrollment.Workshop.EnrolledCount = Math.Max(0, enrollment.Workshop.EnrolledCount - 1);

        await db.SaveChangesAsync();

        return NoContent();
    }

    // Employer başvuruyu onaylar → "pending" → "confirmed"
    [HttpPatch("{id}/approve")]
    [Authorize(Roles = "employer")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var enrollment = await db.Enrollments
            .Include(e => e.Workshop)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (enrollment == null)
            return NotFound();

        if (enrollment.Workshop.EmployerId != currentUser.UserId)
            return Forbid();

        if (enrollment.Status != "pending")
            return BadRequest(new { message = "Sadece bekleyen başvurular onaylanabilir." });

        if (enrollment.Workshop.EnrolledCount >= enrollment.Workshop.Capacity)
            return BadRequest(new { message = "Atölye kapasitesi dolu." });

        enrollment.Status = "confirmed";
        enrollment.TicketCode = Guid.NewGuid().ToString("N");
        enrollment.Workshop.EnrolledCount += 1;

        await db.SaveChangesAsync();

        await reminderService.CreateRemindersAsync(
            enrollment.UserId, ReminderSourceType.Workshop, enrollment.WorkshopId, enrollment.Workshop.StartAt);

        await notifications.NotifyAsync(
            userId:    enrollment.UserId,
            type:      NotificationType.ApplicationApproved,
            title:     "Kaydınız onaylandı!",
            body:      $"\"{enrollment.Workshop.Title}\" atölyesine katılım talebiniz onaylandı.",
            metadata:  new { workshopId = enrollment.WorkshopId, route = "workshop/detail" },
            sendEmail: true);
        return Ok(new
        {
            id = enrollment.Id,
            status = enrollment.Status,
            ticketCode = enrollment.TicketCode
        });
    }

    // Employer başvuruyu reddeder → "pending" → "cancelled"
    [HttpPatch("{id}/reject")]
    [Authorize(Roles = "employer")]
    public async Task<IActionResult> Reject(Guid id)
    {
        var enrollment = await db.Enrollments
            .Include(e => e.Workshop)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (enrollment == null)
            return NotFound();

        if (enrollment.Workshop.EmployerId != currentUser.UserId)
            return Forbid();

        if (enrollment.Status != "pending")
            return BadRequest(new { message = "Sadece bekleyen başvurular reddedilebilir." });

        enrollment.Status = "cancelled";
        await db.SaveChangesAsync();

        await notifications.NotifyAsync(
            userId:    enrollment.UserId,
            type:      NotificationType.ApplicationRejected,
            title:     "Kayıt talebiniz reddedildi",
            body:      $"\"{enrollment.Workshop.Title}\" atölyesine katılım talebiniz maalesef reddedildi.",
            metadata:  new { workshopId = enrollment.WorkshopId },
            sendEmail: true);

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

        var employee = await db.Users.FirstAsync(u => u.Id == enrollment.UserId);
        employee.XpPoints += 25;

        await db.SaveChangesAsync();

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

    // Employer: kendi atölyelerine gelen başvurular listesi
    [HttpGet("/api/v1/employer/enrollments")]
    [Authorize(Roles = "employer")]
    public async Task<IActionResult> GetEmployerEnrollments([FromQuery] string? status)
    {
        var query = db.Enrollments
            .Include(e => e.Workshop)
            .Include(e => e.User)
            .Where(e => e.Workshop.EmployerId == currentUser.UserId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && status != "all")
            query = query.Where(e => e.Status == status);

        var result = await query
            .OrderByDescending(e => e.EnrolledAt)
            .Select(e => new EmployerEnrollmentResponse
            {
                Id = e.Id,
                WorkshopTitle = e.Workshop.Title,
                EmployeeName = e.User.FullName,
                Status = e.Status,
                AppliedAt = e.EnrolledAt,
                Message = null
            })
            .ToListAsync();

        return Ok(result);
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