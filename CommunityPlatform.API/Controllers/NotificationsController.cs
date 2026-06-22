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
public class NotificationsController(AppDbContext db, ICurrentUserService currentUser) : ControllerBase
{
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
                Type = n.Type,
                Title = n.Title,
                Body = n.Body,
                Channel = n.Channel,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return Ok(notifications);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var count = await db.Notifications
            .CountAsync(n => n.UserId == currentUser.UserId && !n.IsRead);

        return Ok(new { unreadCount = count });
    }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == currentUser.UserId);

        if (notification == null)
            return NotFound();

        notification.IsRead = true;
        notification.SentAt ??= DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { id = notification.Id, isRead = notification.IsRead });
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var unread = await db.Notifications
            .Where(n => n.UserId == currentUser.UserId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
        {
            n.IsRead = true;
            n.SentAt ??= DateTime.UtcNow;
        }

        await db.SaveChangesAsync();

        return Ok(new { markedRead = unread.Count });
    }
}