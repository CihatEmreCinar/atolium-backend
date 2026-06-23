using CommunityPlatform.Application.DTOs.Notifications;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityPlatform.API.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController(
    AppDbContext db,
    ICurrentUserService currentUser) : ControllerBase
{
    // GET /api/v1/notifications?page=1&limit=20
    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var notifications = await db.Notifications
            .Where(n => n.UserId == currentUser.UserId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(n => new NotificationResponse
            {
                Id = n.Id,
                Type = n.Type.ToString(),       // enum → string (frontend sabit stringe göre ikonlar çizer)
                Title = n.Title,
                Body = n.Body,
                Channel = n.Channel,
                Metadata = n.Metadata,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return Ok(notifications);
    }

    // GET /api/v1/notifications/unread-count
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var count = await db.Notifications
            .CountAsync(n => n.UserId == currentUser.UserId && !n.IsRead);

        return Ok(new { unreadCount = count });
    }

    // PATCH /api/v1/notifications/{id}/read
    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == currentUser.UserId);

        if (notification == null)
            return NotFound();

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;  // SentAt değil, ReadAt
            await db.SaveChangesAsync();
        }

        return Ok(new { id = notification.Id, isRead = notification.IsRead, readAt = notification.ReadAt });
    }

    // PATCH /api/v1/notifications/read-all
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var now = DateTime.UtcNow;

        var unread = await db.Notifications
            .Where(n => n.UserId == currentUser.UserId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = now;
        }

        await db.SaveChangesAsync();

        return Ok(new { markedRead = unread.Count });
    }

    // DELETE /api/v1/notifications/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == currentUser.UserId);

        if (notification == null)
            return NotFound();

        db.Notifications.Remove(notification);
        await db.SaveChangesAsync();

        return NoContent();
    }
}
