using CommunityPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace CommunityPlatform.Infrastructure.Storage;

public class LocalStorageProvider(
    IWebHostEnvironment env,
    IConfiguration config) : IStorageProvider
{
    private readonly string _basePath = Path.Combine(
        env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"),
        "uploads");
    private readonly string _basePathWithSeparator = Path.GetFullPath(Path.Combine(
        env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"),
        "uploads")) + Path.DirectorySeparatorChar;
    private readonly string _baseUrl = config["Storage:BaseUrl"] ?? "/uploads";

    public async Task<StorageResult> SaveAsync(string key, Stream content, string contentType)
    {
        var fullPath = ResolvePath(key);
        var dir = Path.GetDirectoryName(fullPath)!;

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        await using var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write);
        await content.CopyToAsync(fs);

        return new StorageResult(key, GetUrl(key), fs.Length);
    }

    public Task DeleteAsync(string key)
    {
        var fullPath = ResolvePath(key);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public string GetUrl(string key) => $"{_baseUrl.TrimEnd('/')}/{key}";

    public string? TryGetKeyFromUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var baseUrl = _baseUrl.TrimEnd('/');

        if (url.StartsWith(baseUrl + "/", StringComparison.OrdinalIgnoreCase))
            return url[(baseUrl.Length + 1)..];

        if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
        {
            var path = absoluteUri.AbsolutePath;
            if (path.StartsWith(baseUrl + "/", StringComparison.OrdinalIgnoreCase))
                return path[(baseUrl.Length + 1)..];
        }

        return null;
    }

    private string ResolvePath(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Dosya anahtarı boş olamaz.", nameof(key));

        var normalizedKey = key.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(normalizedKey))
            throw new ArgumentException("Dosya anahtarı kök dizin belirtemez.", nameof(key));

        var fullPath = Path.GetFullPath(Path.Combine(_basePath, normalizedKey));
        if (!fullPath.StartsWith(_basePathWithSeparator, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Dosya anahtarı yükleme dizini dışına çıkamaz.", nameof(key));

        return fullPath;
    }
}
