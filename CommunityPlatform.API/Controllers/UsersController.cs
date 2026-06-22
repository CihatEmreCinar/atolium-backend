using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityPlatform.API.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public class UsersController(AppDbContext db, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        if (currentUser.UserId == null)
            return Unauthorized();

        var user = await db.Users
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.Role,
                u.City,
                u.Bio,
                u.AvatarUrl,
                u.XpPoints,
                u.RankLevel,
                u.IsVerified,
                u.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound();

        return Ok(user);
    }
}