using CommunityPlatform.Application.DTOs.Posts;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommunityPlatform.API.Controllers;

[ApiController]
[Route("api/v1/posts")]
[Authorize]
public class PostsController(PostService postService) : ControllerBase
{
    // ─── Post CRUD ───────────────────────────────────────────────────────────

    /// <summary>POST /api/v1/posts — yeni post oluştur</summary>
    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest req)
    {
        var result = await postService.CreatePostAsync(req);
        return CreatedAtAction(nameof(GetPost), new { id = result.Id }, result);
    }

    /// <summary>GET /api/v1/posts/{id}</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPost(Guid id)
    {
        var result = await postService.GetPostByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>PATCH /api/v1/posts/{id} — caption + tag güncelle</summary>
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdatePost(Guid id, [FromBody] UpdatePostRequest req)
    {
        var result = await postService.UpdatePostAsync(id, req);
        return Ok(result);
    }

    /// <summary>DELETE /api/v1/posts/{id}</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePost(Guid id)
    {
        await postService.DeletePostAsync(id);
        return NoContent();
    }

    // ─── Media Upload (2.2) ──────────────────────────────────────────────────

    /// <summary>
    /// POST /api/v1/posts/{id}/media
    /// Dosyayı doğrudan alır, storage'a kaydeder, PostMedia kaydı oluşturur.
    /// Local storage için tek adım; S3'e geçilince pre-signed URL akışına dönüştürülür.
    /// </summary>
    [HttpPost("{id:guid}/media")]
    [RequestSizeLimit(52_428_800)] // 50 MB
    public async Task<IActionResult> UploadMedia(
        Guid id,
        IFormFile file,
        [FromQuery] short orderIndex = 0)
    {
        var result = await postService.PrepareMediaAsync(id, file, orderIndex);
        return Ok(result);
    }

    /// <summary>DELETE /api/v1/posts/{postId}/media/{mediaId}</summary>
    [HttpDelete("{postId:guid}/media/{mediaId:guid}")]
    public async Task<IActionResult> DeleteMedia(Guid postId, Guid mediaId)
    {
        await postService.DeleteMediaAsync(postId, mediaId);
        return NoContent();
    }

    // ─── Kullanıcı postları + sosyal istatistikler ──────────────────────────────
    // NOT: route /users/{userId}/... — bu yüzden ayrı bir route prefix gerekiyor,
    // [Route("api/v1/posts")] sınıf seviyesindeki prefix'i ezmek için tam path veriyoruz.

    /// <summary>GET /api/v1/users/{userId}/posts?cursor=&limit=15</summary>
    [HttpGet("/api/v1/users/{userId:guid}/posts")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserPosts(
        Guid userId,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 15)
    {
        var result = await postService.GetUserPostsAsync(userId, cursor, limit);
        return Ok(result);
    }

    /// <summary>GET /api/v1/users/{userId}/social-stats</summary>
    [HttpGet("/api/v1/users/{userId:guid}/social-stats")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserSocialStats(Guid userId)
    {
        var result = await postService.GetUserSocialStatsAsync(userId);
        return Ok(result);
    }
}
