using CommunityPlatform.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommunityPlatform.API.Controllers;

[ApiController]
[Route("api/v1/tags")]
public class TagsController(TagService tagService) : ControllerBase
{
    /// <summary>GET /api/v1/tags/search?q=sera&limit=10</summary>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchTags(
        [FromQuery] string q,
        [FromQuery] int limit = 10)
    {
        var result = await tagService.SearchTagsAsync(q, limit);
        return Ok(result);
    }

    /// <summary>GET /api/v1/tags/trending?limit=20</summary>
    [HttpGet("trending")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTrending([FromQuery] int limit = 20)
    {
        var result = await tagService.GetTrendingTagsAsync(limit);
        return Ok(result);
    }

    /// <summary>GET /api/v1/tags/{slug}/posts?cursor=&limit=20</summary>
    [HttpGet("{slug}/posts")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTagFeed(
        string slug,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 20)
    {
        var result = await tagService.GetTagFeedAsync(slug, cursor, limit);
        return Ok(result);
    }
}
