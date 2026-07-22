using CommunityPlatform.Application.DTOs.Auth;
using CommunityPlatform.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace CommunityPlatform.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting("auth")]
public class AuthController(AuthService authService, ILogger<AuthController> logger) : ControllerBase
{
   [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var created = await authService.RegisterAsync(request);
            if (!created)
                return Conflict(new { message = "Bu e-posta adresi zaten kayıtlı." });

            return Accepted(new { message = "Hesap oluşturuldu. Devam etmek için e-posta adresinizi doğrulayın." });
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

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var usesOtp = string.IsNullOrWhiteSpace(request.Token);
        var tokenLooksLikeOtp = request.Token is { Length: 6 } && request.Token.All(char.IsAsciiDigit);
        logger.LogInformation(
            "Email verification request received. Mode: {Mode}; Email: {Email}; TokenLength: {TokenLength}; TokenLooksLikeOtp: {TokenLooksLikeOtp}; CodeLength: {CodeLength}; CodeIsSixDigits: {CodeIsSixDigits}",
            usesOtp ? "otp" : "token",
            request.Email?.Trim().ToLowerInvariant(),
            request.Token?.Length,
            tokenLooksLikeOtp,
            request.Code?.Length,
            request.Code is { Length: 6 } && request.Code.All(char.IsAsciiDigit));

        if (tokenLooksLikeOtp && string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Code))
        {
            logger.LogWarning("A six-digit value was submitted as a verification token instead of an OTP code.");
            return BadRequest(new { message = "OTP doğrulaması için e-posta ve code alanlarını gönderin." });
        }

        var verified = usesOtp
            ? await authService.ConfirmEmailOtpAsync(request.Email!, request.Code!)
            : await authService.ConfirmEmailAsync(request.Token!);

        return verified
            ? Ok(new { message = "E-posta adresi doğrulandı." })
            : BadRequest(new { message = "Doğrulama kodu veya bağlantısı geçersiz ya da süresi dolmuş." });
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] RequestPasswordResetRequest request)
    {
        await authService.RequestEmailVerificationAsync(request.Email);
        return Accepted(new { message = "Hesap varsa doğrulama e-postası gönderilecektir." });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] RequestPasswordResetRequest request)
    {
        await authService.RequestPasswordResetAsync(request.Email);
        return Accepted(new { message = "Hesap varsa parola sıfırlama e-postası gönderilecektir." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request) =>
        await authService.ResetPasswordAsync(request.Token, request.NewPassword)
            ? Ok(new { message = "Şifre güncellendi. Lütfen yeniden giriş yapın." })
            : BadRequest(new { message = "Sıfırlama bağlantısı geçersiz veya süresi dolmuş." });
}
