using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityPlatform.API.Controllers;

[ApiController]
[Route("api/v1/categories")]
[AllowAnonymous]
public class CategoriesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await db.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new { c.Id, c.Name, c.Slug, c.IconUrl })
            .ToListAsync();

        return Ok(categories);
    }
}