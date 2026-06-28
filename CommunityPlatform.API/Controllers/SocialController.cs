using CommunityPlatform.Application.DTOs.Social;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommunityPlatform.API.Controllers;

[ApiController]
[Authorize]
public class SocialController(SocialService socialService) : ControllerBase
{
    // ─── Like ────────────────────────────────────────────────────────────────

    /// <summary>POST /api/v1/posts/{id}/like — toggle (idempotent)</summary>
    [HttpPost("api/v1/posts/{id:guid}/like")]
    public async Task<IActionResult> ToggleLike(Guid id)
    {
        var result = await socialService.ToggleLikeAsync(id);
        return Ok(result);
    }

    // ─── Comment ─────────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/posts/{id}/comments?cursor=&limit=20</summary>
    [HttpGet("api/v1/posts/{id:guid}/comments")]
    [AllowAnonymous]
    public async Task<IActionResult> GetComments(
        Guid id,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 20)
    {
        var result = await socialService.GetCommentsAsync(id, cursor, limit);
        return Ok(result);
    }

    /// <summary>POST /api/v1/posts/{id}/comments</summary>
    [HttpPost("api/v1/posts/{id:guid}/comments")]
    public async Task<IActionResult> CreateComment(Guid id, [FromBody] CreateCommentRequest req)
    {
        var result = await socialService.CreateCommentAsync(id, req);
        return Ok(result);
    }

    /// <summary>DELETE /api/v1/comments/{commentId}</summary>
    [HttpDelete("api/v1/comments/{commentId:guid}")]
    public async Task<IActionResult> DeleteComment(Guid commentId)
    {
        await socialService.DeleteCommentAsync(commentId);
        return NoContent();
    }

    // ─── Follow ──────────────────────────────────────────────────────────────

    /// <summary>POST /api/v1/users/{userId}/follow — toggle follow/unfollow</summary>
    [HttpPost("api/v1/users/{userId:guid}/follow")]
    public async Task<IActionResult> ToggleFollow(Guid userId)
    {
        var result = await socialService.ToggleFollowAsync(userId);
        return Ok(result);
    }

    /// <summary>GET /api/v1/users/{userId}/followers?cursor=&limit=20</summary>
    [HttpGet("api/v1/users/{userId:guid}/followers")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFollowers(
        Guid userId,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 20)
    {
        var result = await socialService.GetFollowersAsync(userId, cursor, limit);
        return Ok(result);
    }

    /// <summary>GET /api/v1/users/{userId}/following?cursor=&limit=20</summary>
    [HttpGet("api/v1/users/{userId:guid}/following")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFollowing(
        Guid userId,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 20)
    {
        var result = await socialService.GetFollowingAsync(userId, cursor, limit);
        return Ok(result);
    }

    // ─── Share ───────────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/posts/{id}/share — share token al veya mevcut olanı döndür</summary>
    [HttpGet("api/v1/posts/{id:guid}/share")]
    public async Task<IActionResult> GetShare(Guid id)
    {
        var result = await socialService.GetOrCreateShareAsync(id);
        return Ok(result);
    }
}
