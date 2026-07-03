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
    private readonly string _baseUrl = config["Storage:BaseUrl"] ?? "/uploads";

    public async Task<StorageResult> SaveAsync(string key, Stream content, string contentType)
    {
        var fullPath = Path.Combine(_basePath, key.Replace('/', Path.DirectorySeparatorChar));
        var dir = Path.GetDirectoryName(fullPath)!;

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await content.CopyToAsync(fs);

        return new StorageResult(key, GetUrl(key), fs.Length);
    }

    public Task DeleteAsync(string key)
    {
        var fullPath = Path.Combine(_basePath, key.Replace('/', Path.DirectorySeparatorChar));
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
}