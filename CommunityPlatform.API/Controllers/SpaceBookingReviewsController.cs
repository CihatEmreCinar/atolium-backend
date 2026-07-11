using CommunityPlatform.Application.DTOs.Reviews;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Domain.Enums;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityPlatform.API.Controllers;

[ApiController]
[Route("api/v1/space-bookings/{bookingId}/reviews")]
public class SpaceBookingReviewsController(AppDbContext db, ICurrentUserService currentUser) : ControllerBase
{
    // Employer, tamamlanmış (Approved + süresi geçmiş) bir rezervasyona yorum yazar
    [HttpPost]
    [Authorize(Roles = "employer")]
    public async Task<IActionResult> Create(Guid bookingId, [FromBody] ReviewRequest request)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        if (request.Rating < 1 || request.Rating > 5)
            return BadRequest(new { message = "Puan 1-5 arasında olmalı." });

        var booking = await db.SpaceBookings
            .Include(b => b.SpaceListing).ThenInclude(l => l.CafeProfile)
            .Include(b => b.EmployerProfile)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
            return NotFound(new { message = "Rezervasyon bulunamadı." });

        if (booking.EmployerProfile.UserId != currentUser.UserId)
            return Forbid();

        if (booking.Status != SpaceBookingStatus.Approved || booking.EndDateTime >= DateTime.UtcNow)
            return BadRequest(new { message = "Sadece tamamlanmış rezervasyonlara yorum yapabilirsiniz." });

        var alreadyReviewed = await db.Reviews
            .AnyAsync(r => r.SpaceBookingId == bookingId && r.UserId == currentUser.UserId);
        if (alreadyReviewed)
            return Conflict(new { message = "Bu rezervasyona zaten yorum yaptınız." });

        var review = new Review
        {
            SpaceBookingId = bookingId,
            WorkshopId = null,
            UserId = currentUser.UserId.Value,
            RevieweeType = RevieweeType.Cafe,
            Rating = request.Rating,
            Comment = request.Comment
        };

        db.Reviews.Add(review);

        // XP: yorum yazma +5
        var employer = await db.Users.FirstAsync(u => u.Id == currentUser.UserId);
        employer.XpPoints += 5;

        // XP: cafe sahibine review alma, 5 yıldızsa bonus
        var cafeUser = await db.Users.FirstAsync(u => u.Id == booking.SpaceListing.CafeProfile.UserId);
        cafeUser.XpPoints += request.Rating == 5 ? 15 : 10;

        await db.SaveChangesAsync();

        // AvgRating ve ReviewCount güncelle
        await RecalculateCafeRating(booking.SpaceListing.CafeProfileId);

        var created = await db.Reviews.Include(r => r.User).FirstAsync(r => r.Id == review.Id);
        return CreatedAtAction(nameof(Create), new { bookingId }, MapToResponse(created));
    }

    // Sahibi 24 saat içinde düzenleyebilir
    [HttpPut("{reviewId}")]
    [Authorize(Roles = "employer")]
    public async Task<IActionResult> Update(Guid bookingId, Guid reviewId, [FromBody] ReviewRequest request)
    {
        var review = await db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.SpaceBookingId == bookingId);
        if (review == null)
            return NotFound();

        if (review.UserId != currentUser.UserId)
            return Forbid();

        if (DateTime.UtcNow - review.CreatedAt > TimeSpan.FromHours(24))
            return BadRequest(new { message = "Yorumlar yalnızca ilk 24 saat içinde düzenlenebilir." });

        if (request.Rating < 1 || request.Rating > 5)
            return BadRequest(new { message = "Puan 1-5 arasında olmalı." });

        review.Rating = request.Rating;
        review.Comment = request.Comment;

        await db.SaveChangesAsync();

        var cafeProfileId = await db.SpaceBookings
            .Where(b => b.Id == bookingId)
            .Select(b => b.SpaceListing.CafeProfileId)
            .FirstAsync();
        await RecalculateCafeRating(cafeProfileId);

        var updated = await db.Reviews.Include(r => r.User).FirstAsync(r => r.Id == reviewId);
        return Ok(MapToResponse(updated));
    }

    // Sahibi veya admin silebilir
    [HttpDelete("{reviewId}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid bookingId, Guid reviewId)
    {
        var review = await db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.SpaceBookingId == bookingId);
        if (review == null)
            return NotFound();

        var isOwner = review.UserId == currentUser.UserId;
        var isAdmin = currentUser.Role == "admin";

        if (!isOwner && !isAdmin)
            return Forbid();

        db.Reviews.Remove(review);
        await db.SaveChangesAsync();

        var cafeProfileId = await db.SpaceBookings
            .Where(b => b.Id == bookingId)
            .Select(b => b.SpaceListing.CafeProfileId)
            .FirstAsync();
        await RecalculateCafeRating(cafeProfileId);

        return NoContent();
    }

    // Cafe kendi rezervasyonuna gelen yoruma cevap verir
    [HttpPost("{reviewId}/reply")]
    [Authorize(Policy = "RequireCafeRole")]
    public async Task<IActionResult> Reply(Guid bookingId, Guid reviewId, [FromBody] ReviewReplyRequest request)
    {
        var booking = await db.SpaceBookings
            .Include(b => b.SpaceListing).ThenInclude(l => l.CafeProfile)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
            return NotFound();

        if (booking.SpaceListing.CafeProfile.UserId != currentUser.UserId)
            return Forbid();

        var review = await db.Reviews.Include(r => r.User).FirstOrDefaultAsync(r => r.Id == reviewId && r.SpaceBookingId == bookingId);
        if (review == null)
            return NotFound();

        if (!string.IsNullOrEmpty(review.EmployerReply))
            return Conflict(new { message = "Bu yoruma zaten cevap verilmiş." });

        review.EmployerReply = request.Reply;

        var cafeUser = await db.Users.FirstAsync(u => u.Id == currentUser.UserId);
        cafeUser.XpPoints += 5;

        await db.SaveChangesAsync();

        return Ok(MapToResponse(review));
    }

    private async Task RecalculateCafeRating(Guid cafeProfileId)
    {
        var cafeProfile = await db.CafeProfiles.FirstAsync(c => c.Id == cafeProfileId);
        var reviews = await db.Reviews
            .Where(r => r.SpaceBooking!.SpaceListing.CafeProfileId == cafeProfileId && r.IsVisible)
            .ToListAsync();

        cafeProfile.ReviewCount = reviews.Count;
        cafeProfile.AvgRating = reviews.Count > 0 ? (decimal)reviews.Average(r => r.Rating) : 0;

        await db.SaveChangesAsync();
    }

    private static CafeReviewResponse MapToResponse(Review r) => new()
    {
        Id = r.Id,
        SpaceBookingId = r.SpaceBookingId!.Value,
        UserId = r.UserId,
        UserName = $"{r.User.FirstName} {r.User.LastName}",
        Rating = r.Rating,
        Comment = r.Comment,
        EmployerReply = r.EmployerReply,
        CreatedAt = r.CreatedAt
    };
}
