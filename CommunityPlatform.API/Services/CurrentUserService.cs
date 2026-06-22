using System.Security.Claims;
using CommunityPlatform.Application.Interfaces;

namespace CommunityPlatform.API.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var value = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User?.FindFirst("sub")?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value
                            ?? User?.FindFirst("email")?.Value;

    public string? Role => User?.FindFirst(ClaimTypes.Role)?.Value;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}