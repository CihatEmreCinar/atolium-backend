using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CommunityPlatform.Application.DTOs.Auth;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Infrastructure.Persistence;
using CommunityPlatform.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CommunityPlatform.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_WhenRoleIsCafe_CreatesCafeProfileAndNormalizesRole()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new AppDbContext(options);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "super-secret-key-for-tests-123456",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:ExpiryMinutes"] = "60",
                ["Auth:PublicBaseUrl"] = "https://app.example.test"
            })
            .Build();

        var jwtService = new JwtService(configuration);
        var authService = new AuthService(db, jwtService, new NullPublisher(), configuration, NullLogger<AuthService>.Instance);

        var result = await authService.RegisterAsync(new RegisterRequest
        {
            Email = "cafe@example.com",
            Password = "Password123!",
            FirstName = "Cafe",
            LastName = "Owner",
            Role = "Cafe"
        });

        Assert.True(result);
        var user = await db.Users.SingleAsync(u => u.Email == "cafe@example.com");
        Assert.Equal("cafe", user.Role);
        Assert.False(user.IsVerified);
        var verificationToken = await db.AccountActionTokens.SingleAsync(t => t.UserId == user.Id);
        Assert.Equal(64, verificationToken.TokenHash.Length);
        Assert.Empty(db.RefreshTokens);

        var cafeProfile = await db.CafeProfiles.SingleOrDefaultAsync(p => p.UserId == user.Id);
        Assert.NotNull(cafeProfile);
        Assert.Equal("Cafe Owner", cafeProfile!.Name);
    }

    [Fact]
    public async Task RefreshAsync_when_a_rotated_token_is_reused_revokes_the_entire_session_family()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "super-secret-key-for-tests-123456",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:ExpiryMinutes"] = "60",
                ["Jwt:RefreshExpiryDays"] = "30",
                ["Auth:PublicBaseUrl"] = "https://app.example.test"
            })
            .Build();
        var authService = new AuthService(db, new JwtService(configuration), new NullPublisher(), configuration, NullLogger<AuthService>.Instance);
        db.Users.Add(new CommunityPlatform.Domain.Entities.User
        {
            Email = "verified@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            FirstName = "Verified",
            LastName = "User",
            Role = "employee",
            IsVerified = true
        });
        await db.SaveChangesAsync();

        var firstSession = await authService.LoginAsync(new LoginRequest { Email = "verified@example.com", Password = "Password123!" });
        Assert.NotNull(firstSession);
        Assert.DoesNotContain(firstSession!.RefreshToken, db.RefreshTokens.Single().TokenHash, StringComparison.Ordinal);

        var rotatedSession = await authService.RefreshAsync(firstSession.RefreshToken);
        Assert.NotNull(rotatedSession);
        var replayResult = await authService.RefreshAsync(firstSession.RefreshToken);

        Assert.Null(replayResult);
        Assert.All(db.RefreshTokens, token => Assert.True(token.IsRevoked));
    }

    [Fact]
    public void GenerateAccessToken_WhenUserRoleIsCafe_UsesLowercaseRoleClaim()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "super-secret-key-for-tests-123456",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:ExpiryMinutes"] = "60"
            })
            .Build();

        var jwtService = new JwtService(configuration);
        var user = new CommunityPlatform.Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = "cafe@example.com",
            PasswordHash = "hash",
            FirstName = "Cafe",
            LastName = "Owner",
            Role = "cafe"
        };

        var token = jwtService.GenerateAccessToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        var roleClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
        Assert.Equal("cafe", roleClaim);
    }

    [Fact]
    public async Task EmailVerificationOtp_IsDeliveredHashedAndSingleUse()
    {
        var (db, service, publisher) = CreateOtpTestService();
        await using (db)
        {
            await service.RegisterAsync(new RegisterRequest
            {
                Email = "otp@example.com", Password = "Password123!", FirstName = "Otp", LastName = "User"
            });

            var action = await db.AccountActionTokens.SingleAsync();
            var code = GetEmailValue(publisher, "verificationCode");
            Assert.Matches("^\\d{6}$", code);
            Assert.NotEqual(code, action.OtpHash);
            Assert.Equal(64, action.OtpHash!.Length);
            Assert.NotNull(action.OtpExpiresAt);
            Assert.Equal(0, action.OtpAttemptCount);

            Assert.True(await service.ConfirmEmailOtpAsync("otp@example.com", code));
            Assert.False(await service.ConfirmEmailOtpAsync("otp@example.com", code));
            Assert.True((await db.Users.SingleAsync()).IsVerified);
            Assert.NotNull(action.OtpUsedAt);
        }
    }

    [Fact]
    public async Task EmailVerificationOtp_ExpiresResendsAndLocksAfterFiveWrongAttempts_WithoutBreakingTokenVerification()
    {
        var (db, service, publisher) = CreateOtpTestService();
        await using (db)
        {
            await service.RegisterAsync(new RegisterRequest
            {
                Email = "resend@example.com", Password = "Password123!", FirstName = "Resend", LastName = "User"
            });

            var first = await db.AccountActionTokens.SingleAsync();
            first.OtpExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
            Assert.False(await service.ConfirmEmailOtpAsync("resend@example.com", GetEmailValue(publisher, "verificationCode")));

            await service.RequestEmailVerificationAsync("resend@example.com");
            var actions = await db.AccountActionTokens.OrderBy(t => t.CreatedAt).ToListAsync();
            Assert.Equal(2, actions.Count);
            Assert.NotNull(actions[0].UsedAt);
            Assert.NotNull(actions[0].OtpUsedAt);
            var currentCode = GetEmailValue(publisher, "verificationCode");

            for (var attempt = 0; attempt < 5; attempt++)
                Assert.False(await service.ConfirmEmailOtpAsync("resend@example.com", "000000" == currentCode ? "000001" : "000000"));

            var current = actions[1];
            Assert.Equal(5, current.OtpAttemptCount);
            Assert.NotNull(current.OtpUsedAt);

            // OTP lockout does not remove the established web-token fallback.
            var verificationUrl = GetEmailValue(publisher, "verificationUrl");
            var token = new Uri(verificationUrl).Query.TrimStart('?').Split('=')[1];
            Assert.True(await service.ConfirmEmailAsync(Uri.UnescapeDataString(token)));
        }
    }

    private static (AppDbContext Db, AuthService Service, CapturingPublisher Publisher) CreateOtpTestService()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "super-secret-key-for-tests-123456",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:ExpiryMinutes"] = "60",
                ["Auth:PublicBaseUrl"] = "https://app.example.test"
            })
            .Build();
        var publisher = new CapturingPublisher();
        return (db, new AuthService(db, new JwtService(configuration), publisher, configuration, NullLogger<AuthService>.Instance), publisher);
    }

    private static string GetEmailValue(CapturingPublisher publisher, string propertyName)
    {
        var emailEvent = Assert.IsType<EmailEventEnvelope>(publisher.Messages.Last());
        return Assert.IsType<string>(emailEvent.Payload.GetType().GetProperty(propertyName)!.GetValue(emailEvent.Payload));
    }

    private sealed class NullPublisher : IRabbitMqPublisher
    {
        public Task PublishAsync<T>(string queue, T message) => Task.CompletedTask;
    }

    private sealed class CapturingPublisher : IRabbitMqPublisher
    {
        public List<object> Messages { get; } = [];

        public Task PublishAsync<T>(string queue, T message)
        {
            Messages.Add(message!);
            return Task.CompletedTask;
        }
    }
}
