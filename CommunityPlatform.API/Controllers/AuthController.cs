using CommunityPlatform.Application.DTOs.Auth;
using CommunityPlatform.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace CommunityPlatform.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(AuthService authService) : ControllerBase
{
   [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await authService.RegisterAsync(request);
            if (result == null)
                return Conflict(new { message = "Bu e-posta adresi zaten kayıtlı." });

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        if (result == null)
            return Unauthorized(new { message = "E-posta veya şifre hatalı." });

        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var result = await authService.RefreshAsync(request.RefreshToken);
        if (result == null)
            return Unauthorized(new { message = "Geçersiz veya süresi dolmuş refresh token." });

        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
    {
        await authService.RevokeAsync(request.RefreshToken);
        return Ok(new { message = "Çıkış yapıldı." });
    }
}