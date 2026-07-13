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
[Route("api/v1/workshops/{workshopId}/reviews")]
public class ReviewsController(AppDbContext db, ICurrentUserService currentUser) : ControllerBase
{
    // Herkes yorumları görebilir (gizli olanlar hariç)
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(Guid workshopId, [FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        var reviews = await db.Reviews
            .Include(r => r.User)
            .Where(r => r.WorkshopId == workshopId && r.IsVisible)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(r => MapToResponse(r))
            .ToListAsync();

        return Ok(reviews);
    }

    // Employee, attended olduğu atölyeye yorum yazar
    [HttpPost]
    [Authorize(Roles = "employee")]
    public async Task<IActionResult> Create(Guid workshopId, [FromBody] ReviewRequest request)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        if (request.Rating < 1 || request.Rating > 5)
            return BadRequest(new { message = "Puan 1-5 arasında olmalı." });

        var workshop = await db.Workshops.FirstOrDefaultAsync(w => w.Id == workshopId);
        if (workshop == null)
            return NotFound(new { message = "Atölye bulunamadı." });

        var enrollment = await db.Enrollments
            .FirstOrDefaultAsync(e => e.WorkshopId == workshopId && e.UserId == currentUser.UserId);

        if (enrollment == null || enrollment.AttendanceStatus != AttendanceStatus.Attended)
            return BadRequest(new { message = "Sadece katıldığınız atölyelere yorum yapabilirsiniz." });

        var alreadyReviewed = await db.Reviews
            .AnyAsync(r => r.WorkshopId == workshopId && r.UserId == currentUser.UserId);
        if (alreadyReviewed)
            return Conflict(new { message = "Bu atölyeye zaten yorum yaptınız." });

        var review = new Review
        {
            WorkshopId = workshopId,
            UserId = currentUser.UserId.Value,
            Rating = request.Rating,
            Comment = request.Comment
        };

        db.Reviews.Add(review);

        // XP: yorum yazma +5
        var employee = await db.Users.FirstAsync(u => u.Id == currentUser.UserId);
        employee.XpPoints += 5;

        // XP: employer review alma, 5 yıldızsa bonus
        var employer = await db.Users.FirstAsync(u => u.Id == workshop.EmployerId);
        employer.XpPoints += request.Rating == 5 ? 15 : 10;

        await db.SaveChangesAsync();

        // AvgRating ve ReviewCount güncelle (Seçenek A: yeniden hesapla)
        await RecalculateWorkshopRating(workshopId);

        var created = await db.Reviews.Include(r => r.User).FirstAsync(r => r.Id == review.Id);
        return CreatedAtAction(nameof(GetAll), new { workshopId }, MapToResponse(created));
    }

    // Sahibi 24 saat içinde düzenleyebilir
    [HttpPut("{reviewId}")]
    [Authorize(Roles = "employee")]
    public async Task<IActionResult> Update(Guid workshopId, Guid reviewId, [FromBody] ReviewRequest request)
    {
        var review = await db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.WorkshopId == workshopId);
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
        await RecalculateWorkshopRating(workshopId);

        var updated = await db.Reviews.Include(r => r.User).FirstAsync(r => r.Id == reviewId);
        return Ok(MapToResponse(updated));
    }

    // Sahibi veya admin silebilir
    [HttpDelete("{reviewId}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid workshopId, Guid reviewId)
    {
        var review = await db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.WorkshopId == workshopId);
        if (review == null)
            return NotFound();

        var isOwner = review.UserId == currentUser.UserId;
        var isAdmin = currentUser.Role == "admin";

        if (!isOwner && !isAdmin)
            return Forbid();

        db.Reviews.Remove(review);
        await db.SaveChangesAsync();
        await RecalculateWorkshopRating(workshopId);

        return NoContent();
    }

    // Employer kendi atölyesine gelen yoruma cevap verir
    [HttpPost("{reviewId}/reply")]
    [Authorize(Roles = "employer")]
    public async Task<IActionResult> Reply(Guid workshopId, Guid reviewId, [FromBody] ReviewReplyRequest request)
    {
        var workshop = await db.Workshops.FirstOrDefaultAsync(w => w.Id == workshopId);
        if (workshop == null)
            return NotFound();

        if (workshop.EmployerId != currentUser.UserId)
            return Forbid();

        var review = await db.Reviews.Include(r => r.User).FirstOrDefaultAsync(r => r.Id == reviewId && r.WorkshopId == workshopId);
        if (review == null)
            return NotFound();

        if (!string.IsNullOrEmpty(review.EmployerReply))
            return Conflict(new { message = "Bu yoruma zaten cevap verilmiş." });

        review.EmployerReply = request.Reply;

        var employer = await db.Users.FirstAsync(u => u.Id == currentUser.UserId);
        employer.XpPoints += 5;

        await db.SaveChangesAsync();

        return Ok(MapToResponse(review));
    }

    private async Task RecalculateWorkshopRating(Guid workshopId)
    {
        var workshop = await db.Workshops.FirstAsync(w => w.Id == workshopId);
        var reviews = await db.Reviews.Where(r => r.WorkshopId == workshopId && r.IsVisible).ToListAsync();

        workshop.ReviewCount = reviews.Count;
        workshop.AvgRating = reviews.Count > 0 ? (decimal)reviews.Average(r => r.Rating) : 0;

        await db.SaveChangesAsync();
    }

    private static ReviewResponse MapToResponse(Review r) => new()
    {
        Id = r.Id,
        WorkshopId = r.WorkshopId!.Value,
        UserId = r.UserId,
        UserName = $"{r.User.FirstName} {r.User.LastName}",
        Rating = r.Rating,
        Comment = r.Comment,
        EmployerReply = r.EmployerReply,
        CreatedAt = r.CreatedAt
    };
}