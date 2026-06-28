using CommunityPlatform.Application.DTOs.Feed;
using CommunityPlatform.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommunityPlatform.API.Controllers;

[ApiController]
[Route("api/v1/feed")]
[Authorize]
public class FeedController(FeedService feedService) : ControllerBase
{
    /// <summary>
    /// GET /api/v1/feed
    /// Takip edilen employer'ların postları, engagement score sıralı.
    /// ?cursor=&limit=20&tags[]=seramik&workshopId=
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetFollowingFeed([FromQuery] FeedRequest req)
    {
        var result = await feedService.GetFollowingFeedAsync(req);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/v1/feed/explore
    /// Tüm postlar — takip edilmeyenler dahil.
    /// ?cursor=&limit=20&tags[]=&workshopId=
    /// </summary>
    [HttpGet("explore")]
    [AllowAnonymous]
    public async Task<IActionResult> GetExploreFeed([FromQuery] FeedRequest req)
    {
        var result = await feedService.GetExploreFeedAsync(req);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/v1/feed/workshop/{workshopId}
    /// Belirli bir workshop'a ait tüm postlar.
    /// </summary>
    [HttpGet("workshop/{workshopId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetWorkshopFeed(
        Guid workshopId,
        [FromQuery] FeedRequest req)
    {
        var result = await feedService.GetWorkshopFeedAsync(workshopId, req);
        return Ok(result);
    }
}
