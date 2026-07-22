using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CommunityPlatform.Infrastructure.Persistence;

/// <summary>Enables repeatable EF migration generation without starting the API host.</summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        var apiDirectory = new[]
        {
            currentDirectory,
            Path.Combine(currentDirectory, "CommunityPlatform.API"),
            Path.Combine(currentDirectory, "..", "CommunityPlatform.API")
        }
        .Select(Path.GetFullPath)
        .FirstOrDefault(path => File.Exists(Path.Combine(path, "appsettings.json")))
        ?? throw new DirectoryNotFoundException(
            "CommunityPlatform.API appsettings.json dosyası bulunamadı.");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets(typeof(AppDbContextFactory).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Default user secrets veya ortam değişkeninde tanımlı olmalıdır.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
