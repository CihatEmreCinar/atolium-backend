using CommunityPlatform.Application.DTOs.SpaceBookings;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Domain.Enums;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CommunityPlatform.API.Controllers;

[ApiController]
[Route("api/v1/space-bookings")]
[Authorize]
public class SpaceBookingsController(
    AppDbContext db,
    ICurrentUserService currentUser,
    INotificationService notifications) : ControllerBase
{
    // Employer bir alan için rezervasyon talebi oluşturur → "Pending"
    [HttpPost]
    [Authorize(Policy = "RequireEmployerRole")]
    public async Task<IActionResult> Create([FromBody] CreateSpaceBookingRequest request)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        if (request.EndDateTime <= request.StartDateTime)
            return BadRequest(new { message = "Bitiş tarihi başlangıç tarihinden sonra olmalı." });

        // Important: resolve the current employer profile by UserId first, then use EmployerProfile.Id for the booking.
        var employerProfile = await db.EmployerProfiles.FirstOrDefaultAsync(e => e.UserId == currentUser.UserId.Value);
        if (employerProfile == null)
            return NotFound(new { message = "Employer profili bulunamadı." });

        var listing = await db.SpaceListings
            .Include(l => l.CafeProfile)
            .FirstOrDefaultAsync(l => l.Id == request.SpaceListingId);

        if (listing == null || !listing.IsActive)
            return NotFound(new { message = "Alan ilanı bulunamadı." });

        var hours = (decimal)(request.EndDateTime - request.StartDateTime).TotalHours;
        var booking = new SpaceBooking
        {
            SpaceListingId = listing.Id,
            EmployerProfileId = employerProfile.Id,
            StartDateTime = request.StartDateTime,
            EndDateTime = request.EndDateTime,
            Status = SpaceBookingStatus.Pending,
            TotalPrice = Math.Round(listing.HourlyPrice * hours, 2),
            Notes = request.Notes?.Trim()
        };

        db.SpaceBookings.Add(booking);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23P01" })
        {
            return Conflict(new { message = "Bu tarih aralığı dolu." });
        }

        await notifications.NotifyAsync(
            userId:    listing.CafeProfile.UserId,
            type:      NotificationType.BookingRequested,
            title:     "Yeni rezervasyon talebi",
            body:      $"\"{listing.Title}\" alanınız için {employerProfile.WorkshopTitle} tarafından rezervasyon talebi oluşturuldu.",
            metadata:  new { bookingId = booking.Id, spaceListingId = listing.Id, employerProfileId = employerProfile.Id },
            sendEmail: true);

        var created = await db.SpaceBookings
            .Include(b => b.SpaceListing).ThenInclude(l => l.CafeProfile)
            .Include(b => b.EmployerProfile).ThenInclude(e => e.User)
            .FirstAsync(b => b.Id == booking.Id);

        return CreatedAtAction(nameof(GetById), new { id = booking.Id }, MapToResponse(created));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var booking = await db.SpaceBookings
            .Include(b => b.SpaceListing).ThenInclude(l => l.CafeProfile)
            .Include(b => b.EmployerProfile).ThenInclude(e => e.User)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
            return NotFound();

        var isOwnerEmployer = booking.EmployerProfile.UserId == currentUser.UserId.Value;
        var isOwnerCafe = booking.SpaceListing.CafeProfile.UserId == currentUser.UserId.Value;

        if (!isOwnerEmployer && !isOwnerCafe)
            return Forbid();

        return Ok(MapToResponse(booking));
    }

    // Employer: kendi oluşturduğu rezervasyon talepleri
    [HttpGet("mine")]
    [Authorize(Policy = "RequireEmployerRole")]
    public async Task<IActionResult> GetMine()
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var employerProfile = await db.EmployerProfiles.FirstOrDefaultAsync(e => e.UserId == currentUser.UserId.Value);
        if (employerProfile == null)
            return NotFound(new { message = "Employer profili bulunamadı." });

        var bookings = await db.SpaceBookings
            .Include(b => b.SpaceListing).ThenInclude(l => l.CafeProfile)
            .Include(b => b.EmployerProfile).ThenInclude(e => e.User)
            .Where(b => b.EmployerProfileId == employerProfile.Id)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return Ok(bookings.Select(MapToResponse));
    }

    // Cafe: kendi ilanlarına gelen rezervasyon talepleri
    [HttpGet("incoming")]
    [Authorize(Policy = "RequireCafeRole")]
    public async Task<IActionResult> GetIncoming()
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var cafeProfile = await db.CafeProfiles.FirstOrDefaultAsync(p => p.UserId == currentUser.UserId.Value);
        if (cafeProfile == null)
            return NotFound(new { message = "Cafe profili bulunamadı." });

        var bookings = await db.SpaceBookings
            .Include(b => b.SpaceListing).ThenInclude(l => l.CafeProfile)
            .Include(b => b.EmployerProfile).ThenInclude(e => e.User)
            .Where(b => b.SpaceListing.CafeProfileId == cafeProfile.Id)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return Ok(bookings.Select(MapToResponse));
    }

    // Cafe rezervasyon talebini onaylar → "Pending" → "Approved"
    [HttpPut("{id}/approve")]
    [Authorize(Policy = "RequireCafeRole")]
    public async Task<IActionResult> Approve(Guid id)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var booking = await db.SpaceBookings
            .Include(b => b.SpaceListing).ThenInclude(l => l.CafeProfile)
            .Include(b => b.EmployerProfile).ThenInclude(e => e.User)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
            return NotFound();

        if (booking.SpaceListing.CafeProfile.UserId != currentUser.UserId.Value)
            return Forbid();

        if (booking.Status != SpaceBookingStatus.Pending)
            return BadRequest(new { message = "Sadece bekleyen talepler onaylanabilir." });

        booking.Status = SpaceBookingStatus.Approved;
        booking.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await notifications.NotifyAsync(
            userId:    booking.EmployerProfile.UserId,
            type:      NotificationType.BookingApproved,
            title:     "Rezervasyon talebiniz onaylandı!",
            body:      $"\"{booking.SpaceListing.Title}\" alanı için rezervasyon talebiniz onaylandı.",
            metadata:  new { bookingId = booking.Id, spaceListingId = booking.SpaceListingId, route = "booking/detail" },
            sendEmail: true);

        return Ok(MapToResponse(booking));
    }

    // Cafe rezervasyon talebini reddeder → "Pending" → "Rejected"
    [HttpPut("{id}/reject")]
    [Authorize(Policy = "RequireCafeRole")]
    public async Task<IActionResult> Reject(Guid id)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var booking = await db.SpaceBookings
            .Include(b => b.SpaceListing).ThenInclude(l => l.CafeProfile)
            .Include(b => b.EmployerProfile).ThenInclude(e => e.User)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
            return NotFound();

        if (booking.SpaceListing.CafeProfile.UserId != currentUser.UserId.Value)
            return Forbid();

        if (booking.Status != SpaceBookingStatus.Pending)
            return BadRequest(new { message = "Sadece bekleyen talepler reddedilebilir." });

        booking.Status = SpaceBookingStatus.Rejected;
        booking.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await notifications.NotifyAsync(
            userId:    booking.EmployerProfile.UserId,
            type:      NotificationType.BookingRejected,
            title:     "Rezervasyon talebiniz reddedildi",
            body:      $"\"{booking.SpaceListing.Title}\" alanı için rezervasyon talebiniz maalesef reddedildi.",
            metadata:  new { bookingId = booking.Id, spaceListingId = booking.SpaceListingId },
            sendEmail: true);

        return Ok(MapToResponse(booking));
    }

    // Employer kendi talebini iptal eder → bildirim gerekmiyor
    [HttpPut("{id}/cancel")]
    [Authorize(Policy = "RequireEmployerRole")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var booking = await db.SpaceBookings
            .Include(b => b.SpaceListing).ThenInclude(l => l.CafeProfile)
            .Include(b => b.EmployerProfile).ThenInclude(e => e.User)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
            return NotFound();

        if (booking.EmployerProfile.UserId != currentUser.UserId.Value)
            return Forbid();

        if (booking.Status is SpaceBookingStatus.Cancelled or SpaceBookingStatus.Rejected)
            return BadRequest(new { message = "Bu talep zaten sonuçlandırılmış." });

        booking.Status = SpaceBookingStatus.Cancelled;
        booking.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(MapToResponse(booking));
    }

    private static SpaceBookingResponse MapToResponse(SpaceBooking b) => new()
    {
        Id = b.Id,
        SpaceListingId = b.SpaceListingId,
        EmployerProfileId = b.EmployerProfileId,
        StartDateTime = b.StartDateTime,
        EndDateTime = b.EndDateTime,
        Status = b.Status.ToString(),
        TotalPrice = b.TotalPrice,
        Notes = b.Notes,
        CreatedAt = b.CreatedAt,
        UpdatedAt = b.UpdatedAt,
        SpaceListingTitle = b.SpaceListing?.Title,
        CafeProfileId = b.SpaceListing?.CafeProfileId,
        CafeName = b.SpaceListing?.CafeProfile?.Name,
        CafeCity = b.SpaceListing?.CafeProfile?.City,
        EmployerWorkshopTitle = b.EmployerProfile?.WorkshopTitle,
        EmployerFullName = b.EmployerProfile?.User?.FullName
    };
}
