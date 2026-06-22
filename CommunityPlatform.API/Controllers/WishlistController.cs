using CommunityPlatform.Application.DTOs.Wishlist;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityPlatform.API.Controllers;

[ApiController]
[Route("api/v1/wishlist")]
[Authorize]
public class WishlistController(AppDbContext db, ICurrentUserService currentUser) : ControllerBase
{
    // Toggle: varsa kaldır, yoksa ekle
    [HttpPost("{workshopId}")]
    public async Task<IActionResult> Toggle(Guid workshopId)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var workshop = await db.Workshops.FirstOrDefaultAsync(w => w.Id == workshopId);
        if (workshop == null)
            return NotFound(new { message = "Atolye bulunamadi." });

        var existing = await db.Wishlists
            .FirstOrDefaultAsync(w => w.UserId == currentUser.UserId && w.WorkshopId == workshopId);

        if (existing != null)
        {
            db.Wishlists.Remove(existing);
            await db.SaveChangesAsync();
            return Ok(new { wishlisted = false, message = "Wishlistten kaldirildi." });
        }

        var wishlist = new Wishlist
        {
            UserId = currentUser.UserId.Value,
            WorkshopId = workshopId
        };

        db.Wishlists.Add(wishlist);
        await db.SaveChangesAsync();

        return Ok(new { wishlisted = true, message = "Wishlisteeklendi." });
    }

    // Kendi wishlist'i
    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var items = await db.Wishlists
            .Include(w => w.Workshop)
                .ThenInclude(ws => ws.Employer)
            .Where(w => w.UserId == currentUser.UserId)
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(w => new WishlistResponse
            {
                Id = w.Id,
                WorkshopId = w.WorkshopId,
                WorkshopTitle = w.Workshop.Title,
                EmployerName = w.Workshop.Employer.FirstName + " " + w.Workshop.Employer.LastName,
                Price = w.Workshop.Price,
                StartAt = w.Workshop.StartAt,
                Status = w.Workshop.Status,
                AvgRating = w.Workshop.AvgRating,
                CreatedAt = w.CreatedAt
            })
            .ToListAsync();

        return Ok(items);
    }
}