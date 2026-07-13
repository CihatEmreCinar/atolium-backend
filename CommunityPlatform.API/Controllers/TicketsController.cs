using CommunityPlatform.Application.DTOs.Tickets;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Domain.Enums;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CommunityPlatform.API.Controllers;

[ApiController]
[Route("api/v1/tickets")]
[Authorize]
public class TicketsController(
    AppDbContext db,
    ICurrentUserService currentUser,
    ITicketSigningService signingService,
    INotificationService notifications) : ControllerBase
{
    // Employer QR'ı tarar, katılımcı bilgisini önizler — HENÜZ attendance işaretlemez.
    // Scan → önizleme ekranı → employer "onayla" der → CheckIn çağrılır.
    [HttpPost("verify")]
    [Authorize(Roles = "employer")]
    [EnableRateLimiting("ticket-verify")]
    public async Task<IActionResult> Verify([FromBody] VerifyTicketRequest request)
    {
        var (ticket, error) = await ResolveAndValidateTicketAsync(request.QrPayload);
        if (error != null) return error;

        var enrollment = ticket!.Enrollment;

        return Ok(new TicketPreviewResponse
        {
            TicketId = ticket.Id,
            EnrollmentId = enrollment.Id,
            ParticipantName = $"{enrollment.User.FirstName} {enrollment.User.LastName}",
            WorkshopTitle = enrollment.Workshop.Title,
            AttendanceStatus = enrollment.AttendanceStatus.ToString(),
            AlreadyUsed = ticket.UsedAt != null,
            UsedAt = ticket.UsedAt
        });
    }

    // Verify'daki tüm kontrolleri tekrar yapar + geçerliyse Attended olarak işaretler.
    // Idempotent DEĞİL: aynı bilet ikinci kez okutulursa 409 döner (replay koruması —
    // bir QR ekran görüntüsü paylaşılsa bile ikinci kez check-in yapılamaz).
    [HttpPost("check-in")]
    [Authorize(Roles = "employer")]
    [EnableRateLimiting("ticket-verify")]
    public async Task<IActionResult> CheckIn([FromBody] VerifyTicketRequest request)
    {
        var (ticket, error) = await ResolveAndValidateTicketAsync(request.QrPayload);
        if (error != null) return error;

        if (ticket!.UsedAt != null)
            return Conflict(new { message = "Bu bilet daha önce okutuldu.", usedAt = ticket.UsedAt });

        var enrollment = ticket.Enrollment;

        ticket.UsedAt = DateTime.UtcNow;
        enrollment.AttendanceStatus = AttendanceStatus.Attended;
        enrollment.AttendedAt = DateTime.UtcNow;

        var employee = await db.Users.FirstAsync(u => u.Id == enrollment.UserId);
        employee.XpPoints += 25;

        await db.SaveChangesAsync();

        await notifications.NotifyAsync(
            userId:    enrollment.UserId,
            type:      NotificationType.WorkshopCompleted,
            title:     "Katılımınız teyit edildi!",
            body:      $"{enrollment.Workshop.Title} atölyesine katılımınız onaylandı. +25 XP kazandınız!",
            metadata:  new { workshopId = enrollment.WorkshopId, route = "workshop/detail" },
            sendEmail: true);

        return Ok(new CheckInResponse
        {
            EnrollmentId = enrollment.Id,
            ParticipantName = $"{enrollment.User.FirstName} {enrollment.User.LastName}",
            AttendanceStatus = enrollment.AttendanceStatus.ToString(),
            AttendedAt = enrollment.AttendedAt!.Value
        });
    }

    // Verify ve CheckIn'in ortak doğrulama zinciri. Kontrol sırası:
    // parse → imza → workshop bu employer'a mı ait → revoke → expire → enrollment confirmed mı.
    // Not: Signature kontrolü ownership kontrolünden ÖNCE yapılır — imzası geçersiz bir QR için
    // "bu workshop size ait değil mi yoksa imza mı yanlış" bilgisini sızdırmamak adına 400 döner.
    private async Task<(WorkshopTicket? Ticket, IActionResult? Error)> ResolveAndValidateTicketAsync(string qrPayload)
    {
        if (string.IsNullOrWhiteSpace(qrPayload))
            return (null, BadRequest(new { message = "Geçersiz QR kodu." }));

        var parts = qrPayload.Split('.', 2);
        if (parts.Length != 2 || !Guid.TryParseExact(parts[0], "N", out var ticketId))
            return (null, BadRequest(new { message = "Geçersiz QR kodu." }));

        var signature = parts[1];

        var ticket = await db.WorkshopTickets
            .Include(t => t.Enrollment).ThenInclude(e => e.Workshop)
            .Include(t => t.Enrollment).ThenInclude(e => e.User)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket == null)
            return (null, NotFound(new { message = "Bilet bulunamadı." }));

        if (!signingService.Verify(ticket.Id, ticket.Nonce, ticket.ExpiresAt, signature))
            return (null, BadRequest(new { message = "Bilet imzası geçersiz." }));

        if (ticket.Enrollment.Workshop.EmployerId != currentUser.UserId)
            return (null, Forbid());

        if (ticket.Revoked)
            return (null, BadRequest(new { message = "Bilet iptal edilmiş." }));

        if (ticket.ExpiresAt < DateTime.UtcNow)
            return (null, BadRequest(new { message = "Bilet süresi dolmuş." }));

        if (ticket.Enrollment.Status != "confirmed")
            return (null, BadRequest(new { message = "Kayıt onaylı değil." }));

        return (ticket, null);
    }
}
