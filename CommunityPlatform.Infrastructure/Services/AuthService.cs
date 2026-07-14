using CommunityPlatform.Application.DTOs.Auth;
using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityPlatform.Infrastructure.Services;

public class AuthService(AppDbContext db, JwtService jwtService)
{
    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        if (await db.Users.AnyAsync(u => u.Email == request.Email))
            return null;

        var normalizedRole = NormalizeRole(request.Role);

        var user = new User
        {
            Email = request.Email.ToLower().Trim(),
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

        return await BuildAuthResponse(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower().Trim());

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        if (!user.IsActive)
            return null;

        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await BuildAuthResponse(user);
    }

    public async Task<AuthResponse?> RefreshAsync(string refreshToken)
    {
        var stored = await db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (stored == null || stored.IsRevoked || stored.ExpiresAt < DateTime.UtcNow)
            return null;

        // Banlanmış (IsActive = false) kullanıcı, elindeki eski refresh token ile
        // süresiz oturum yenileyemesin. Login zaten bunu engelliyordu, refresh etmiyordu.
        if (!stored.User.IsActive)
            return null;

        // Eski token'ı iptal et (rotation)
        stored.IsRevoked = true;

        return await BuildAuthResponse(stored.User);
    }

    public async Task<bool> RevokeAsync(string refreshToken)
    {
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);
        if (stored == null)
            return false;

        stored.IsRevoked = true;
        await db.SaveChangesAsync();
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

    private async Task<AuthResponse> BuildAuthResponse(User user)
    {
        var accessToken = jwtService.GenerateAccessToken(user);
        var refreshTokenValue = jwtService.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(60);

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        });

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
}