using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CommunityPlatform.Application.DTOs.Auth;
using CommunityPlatform.Infrastructure.Persistence;
using CommunityPlatform.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

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
                ["Jwt:ExpiryMinutes"] = "60"
            })
            .Build();

        var jwtService = new JwtService(configuration);
        var authService = new AuthService(db, jwtService);

        var result = await authService.RegisterAsync(new RegisterRequest
        {
            Email = "cafe@example.com",
            Password = "Password123!",
            FirstName = "Cafe",
            LastName = "Owner",
            Role = "Cafe"
        });

        Assert.NotNull(result);
        var user = await db.Users.SingleAsync(u => u.Email == "cafe@example.com");
        Assert.Equal("cafe", user.Role);

        var cafeProfile = await db.CafeProfiles.SingleOrDefaultAsync(p => p.UserId == user.Id);
        Assert.NotNull(cafeProfile);
        Assert.Equal("Cafe Owner", cafeProfile!.Name);
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
}
