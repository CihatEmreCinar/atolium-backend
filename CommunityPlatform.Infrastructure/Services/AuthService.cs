using CommunityPlatform.Application.DTOs.Auth;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace CommunityPlatform.Infrastructure.Services;

public class AuthService(
    AppDbContext db,
    JwtService jwtService,
    IRabbitMqPublisher publisher,
    IConfiguration configuration,
    ILogger<AuthService> logger)
{
    private readonly string _publicBaseUrl = RequirePublicBaseUrl(configuration);

    public async Task<bool> RegisterAsync(RegisterRequest request)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        if (await db.Users.AnyAsync(u => u.Email == normalizedEmail))
            return false;

        var normalizedRole = NormalizeRole(request.Role);

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = normalizedRole,
            CityId = request.CityId,
            DistrictId = request.DistrictId
        };

        db.Users.Add(user);

        if (normalizedRole == "employer")
            db.EmployerProfiles.Add(new EmployerProfile { UserId = user.Id, WorkshopTitle = "" });
        else if (normalizedRole == "employee")
            db.EmployeeProfiles.Add(new EmployeeProfile { UserId = user.Id });
        else if (normalizedRole == "cafe")
            db.CafeProfiles.Add(new CafeProfile { UserId = user.Id, Name = string.IsNullOrWhiteSpace(user.FirstName + " " + user.LastName) ? user.Email : $"{user.FirstName} {user.LastName}".Trim() });

        await db.SaveChangesAsync();
        await SendEmailVerificationAsync(user);
        return true;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == NormalizeEmail(request.Email));

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        if (!user.IsActive || !user.IsVerified)
            return null;

        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await BuildAuthResponse(user);
    }

    public async Task<AuthResponse?> RefreshAsync(string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);
        var stored = await db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

        if (stored == null || stored.ExpiresAt < DateTime.UtcNow)
            return null;

        if (stored.IsRevoked)
        {
            await RevokeFamilyAsync(stored.UserId, stored.FamilyId, "Refresh token reuse detected");
            return null;
        }

        // Banlanmış (IsActive = false) kullanıcı, elindeki eski refresh token ile
        // süresiz oturum yenileyemesin. Login zaten bunu engelliyordu, refresh etmiyordu.
        if (!stored.User.IsActive)
            return null;

        // Eski token'ı iptal et (rotation)
        stored.IsRevoked = true;
        stored.RevokedAt = DateTime.UtcNow;
        stored.RevocationReason = "Rotated";

        return await BuildAuthResponse(stored.User, stored.FamilyId, stored);
    }

    public async Task<bool> RevokeAsync(string refreshToken)
    {
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == HashToken(refreshToken));
        if (stored == null)
            return false;

        await RevokeFamilyAsync(stored.UserId, stored.FamilyId, "User logout");
        return true;
    }

    public async Task<bool> ConfirmEmailAsync(string token)
    {
        var action = await FindUsableActionTokenAsync(token, AccountActionTokenPurposes.EmailVerification);
        if (action == null) return false;

        MarkEmailVerificationComplete(action, DateTime.UtcNow);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ConfirmEmailOtpAsync(string email, string code)
    {
        var now = DateTime.UtcNow;
        var user = await FindActiveUserAsync(email);
        if (user == null || user.IsVerified)
        {
            logger.LogWarning(
                "OTP verification rejected before token lookup. Email: {Email}; UserFound: {UserFound}; IsVerified: {IsVerified}; UtcNow: {UtcNow}",
                NormalizeEmail(email), user is not null, user?.IsVerified, now);
            return false;
        }

        var action = await db.AccountActionTokens
            .Include(t => t.User)
            .Where(t => t.UserId == user.Id
                && t.Purpose == AccountActionTokenPurposes.EmailVerification
                && t.UsedAt == null
                && t.OtpUsedAt == null
                && t.OtpExpiresAt > now)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();

        if (action == null)
        {
            logger.LogWarning(
                "OTP verification token lookup returned no eligible row. Email: {Email}; UserId: {UserId}; Purpose: {Purpose}; UtcNow: {UtcNow}",
                user.Email, user.Id, AccountActionTokenPurposes.EmailVerification, now);
            return false;
        }

        logger.LogInformation(
            "OTP verification token loaded. Email: {Email}; TokenId: {TokenId}; Purpose: {Purpose}; OtpHashPresent: {OtpHashPresent}; OtpExpiresAt: {OtpExpiresAt}; OtpUsedAt: {OtpUsedAt}; OtpAttemptCount: {OtpAttemptCount}; UsedAt: {UsedAt}; UtcNow: {UtcNow}; SubmittedCodeLength: {SubmittedCodeLength}; SubmittedCodeIsSixDigits: {SubmittedCodeIsSixDigits}",
            user.Email, action.Id, action.Purpose, !string.IsNullOrWhiteSpace(action.OtpHash), action.OtpExpiresAt,
            action.OtpUsedAt, action.OtpAttemptCount, action.UsedAt, now, code.Length,
            code.Length == 6 && code.All(char.IsAsciiDigit));

        var submittedHash = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        var storedHash = Convert.FromHexString(action.OtpHash!);
        if (!CryptographicOperations.FixedTimeEquals(storedHash, submittedHash))
        {
            action.OtpAttemptCount++;
            if (action.OtpAttemptCount >= MaxOtpAttempts)
                action.OtpUsedAt = now;

            await db.SaveChangesAsync();
            logger.LogWarning(
                "OTP verification hash comparison failed. Email: {Email}; TokenId: {TokenId}; OtpAttemptCount: {OtpAttemptCount}; Locked: {Locked}",
                user.Email, action.Id, action.OtpAttemptCount, action.OtpUsedAt is not null);
            return false;
        }

        MarkEmailVerificationComplete(action, now);
        await db.SaveChangesAsync();
        logger.LogInformation("OTP verification succeeded. Email: {Email}; TokenId: {TokenId}", user.Email, action.Id);
        return true;
    }

    public async Task RequestEmailVerificationAsync(string email)
    {
        var user = await FindActiveUserAsync(email);
        if (user is not null && !user.IsVerified)
            await SendEmailVerificationAsync(user);
    }

    public async Task RequestPasswordResetAsync(string email)
    {
        var user = await FindActiveUserAsync(email);
        if (user is null) return;

        var token = await CreateActionTokenAsync(user.Id, AccountActionTokenPurposes.PasswordReset, TimeSpan.FromHours(1));
        await PublishEmailAsync("PasswordResetEvent", new
        {
            toEmail = user.Email,
            toName = user.FullName,
            displayName = user.FirstName,
            resetUrl = BuildActionUrl("reset-password", token),
            expiresIn = TimeSpan.FromHours(1)
        });
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var action = await FindUsableActionTokenAsync(token, AccountActionTokenPurposes.PasswordReset);
        if (action == null) return false;

        action.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        action.UsedAt = DateTime.UtcNow;
        await RevokeAllUserSessionsAsync(action.UserId, "Password reset");
        await db.SaveChangesAsync();

        await PublishEmailAsync("PasswordChangedEvent", new
        {
            toEmail = action.User.Email,
            toName = action.User.FullName,
            displayName = action.User.FirstName,
            changedAtUtc = DateTimeOffset.UtcNow
        });
        return true;
    }

    // GÜVENLİK: "admin" burada KASITLI olarak kabul edilmiyor. Genel /auth/register
    // ucu üzerinden kimse admin rolüyle kayıt olamaz — admin ataması yalnızca
    // AdminController.PromoteToAdmin üzerinden, zaten admin olan biri tarafından yapılabilir.
    private static string NormalizeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return "employee";

        var normalized = role.Trim().ToLowerInvariant();

        return normalized switch
        {
            "employer" => "employer",
            "employee" => "employee",
            "cafe" => "cafe",
            "admin" => throw new ArgumentException("Bu rolle kayıt oluşturulamaz."),
            _ => throw new ArgumentException($"Geçersiz rol: '{role}'. Beklenen: employer, employee, cafe.")
        };
    }

    private async Task<AuthResponse> BuildAuthResponse(User user, Guid? familyId = null, RefreshToken? replacedToken = null)
    {
        var accessToken = jwtService.GenerateAccessToken(user);
        var refreshTokenValue = jwtService.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(GetPositiveInt("Jwt:ExpiryMinutes", 60));

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            FamilyId = familyId ?? Guid.NewGuid(),
            TokenHash = HashToken(refreshTokenValue),
            ExpiresAt = DateTime.UtcNow.AddDays(GetPositiveInt("Jwt:RefreshExpiryDays", 30))
        };

        db.RefreshTokens.Add(refreshToken);
        if (replacedToken is not null)
            replacedToken.ReplacedByTokenId = refreshToken.Id;

        await db.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = expiresAt,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                XpPoints = user.XpPoints,
                RankLevel = user.RankLevel
            }
        };
    }

    private async Task SendEmailVerificationAsync(User user)
    {
        var token = await CreateActionTokenAsync(user.Id, AccountActionTokenPurposes.EmailVerification, TimeSpan.FromHours(24));
        var otp = await CreateEmailVerificationOtpAsync(user.Id, token);
        await PublishEmailAsync("VerifyEmailEvent", new
        {
            toEmail = user.Email,
            toName = user.FullName,
            displayName = user.FirstName,
            verificationUrl = BuildActionUrl("verify-email", token),
            expiresIn = TimeSpan.FromHours(24),
            verificationCode = otp,
            otpExpiresIn = OtpLifetime
        });
    }

    private async Task<string> CreateActionTokenAsync(Guid userId, string purpose, TimeSpan lifetime)
    {
        var now = DateTime.UtcNow;
        var activeTokens = await db.AccountActionTokens
            .Where(t => t.UserId == userId && t.Purpose == purpose && t.UsedAt == null && t.ExpiresAt > now)
            .ToListAsync();
        foreach (var token in activeTokens)
        {
            token.UsedAt = now;
            if (token.Purpose == AccountActionTokenPurposes.EmailVerification && token.OtpHash != null)
                token.OtpUsedAt = now;
        }

        var rawToken = jwtService.GenerateRefreshToken();
        db.AccountActionTokens.Add(new AccountActionToken
        {
            UserId = userId,
            TokenHash = HashToken(rawToken),
            Purpose = purpose,
            ExpiresAt = now.Add(lifetime)
        });
        await db.SaveChangesAsync();
        return rawToken;
    }

    private async Task<string> CreateEmailVerificationOtpAsync(Guid userId, string rawToken)
    {
        var action = await db.AccountActionTokens.SingleAsync(t =>
            t.UserId == userId &&
            t.Purpose == AccountActionTokenPurposes.EmailVerification &&
            t.TokenHash == HashToken(rawToken));

        var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        action.OtpHash = HashToken(otp);
        action.OtpExpiresAt = DateTime.UtcNow.Add(OtpLifetime);
        action.OtpAttemptCount = 0;
        action.OtpUsedAt = null;
        await db.SaveChangesAsync();
        return otp;
    }

    private async Task<AccountActionToken?> FindUsableActionTokenAsync(string token, string purpose) =>
        await db.AccountActionTokens.Include(t => t.User).FirstOrDefaultAsync(t =>
            t.TokenHash == HashToken(token) && t.Purpose == purpose && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow);

    private static void MarkEmailVerificationComplete(AccountActionToken action, DateTime now)
    {
        action.UsedAt = now;
        action.OtpUsedAt = now;
        action.User.IsVerified = true;
    }

    private async Task<User?> FindActiveUserAsync(string email) =>
        await db.Users.FirstOrDefaultAsync(u => u.Email == NormalizeEmail(email) && u.IsActive);

    private async Task RevokeFamilyAsync(Guid userId, Guid familyId, string reason)
    {
        var now = DateTime.UtcNow;
        var tokens = await db.RefreshTokens.Where(t => t.UserId == userId && t.FamilyId == familyId && !t.IsRevoked).ToListAsync();
        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = now;
            token.RevocationReason = reason;
        }
        await db.SaveChangesAsync();
    }

    private async Task RevokeAllUserSessionsAsync(Guid userId, string reason)
    {
        var now = DateTime.UtcNow;
        var tokens = await db.RefreshTokens.Where(t => t.UserId == userId && !t.IsRevoked).ToListAsync();
        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = now;
            token.RevocationReason = reason;
        }
    }

    private async Task PublishEmailAsync(string eventType, object payload)
    {
        try
        {
            await publisher.PublishAsync("notification.email", new EmailEventEnvelope(eventType, payload));
        }
        catch (Exception ex)
        {
            // Outbox is introduced in the next reliability PR. The account operation remains valid
            // and clients can use the resend endpoint if broker delivery is temporarily unavailable.
            logger.LogError(ex, "Account e-mail event could not be published: {EventType}", eventType);
        }
    }

    private string BuildActionUrl(string action, string token) =>
        $"{_publicBaseUrl}/{action}?token={Uri.EscapeDataString(token)}";

    private int GetPositiveInt(string key, int fallback) =>
        int.TryParse(configuration[key], out var value) && value > 0 ? value : fallback;

    private static string RequirePublicBaseUrl(IConfiguration configuration)
    {
        var value = configuration["Auth:PublicBaseUrl"];
        if (string.IsNullOrWhiteSpace(value) || !Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Auth:PublicBaseUrl geçerli bir HTTPS URL olmalıdır.");
        return uri.ToString().TrimEnd('/');
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private const int MaxOtpAttempts = 5;
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);
}
