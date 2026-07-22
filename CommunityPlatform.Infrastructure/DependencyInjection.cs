using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Infrastructure.Persistence;
using CommunityPlatform.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddScoped<JwtService>();
        services.AddScoped<AuthService>();
        services.AddScoped<SafeUploadService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPushNotificationSender, PushNotificationSender>();
        services.AddScoped<IQrCodeGenerator, QrCodeGenerator>();

        return services;
    }
}
