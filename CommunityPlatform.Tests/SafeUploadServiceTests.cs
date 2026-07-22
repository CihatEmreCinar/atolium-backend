using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Infrastructure.Services;

namespace CommunityPlatform.Tests;

public class SafeUploadServiceTests
{
    [Fact]
    public async Task SaveImageAsync_rejects_content_without_an_allowed_signature()
    {
        var content = new MemoryStream("<script>alert(1)</script>"u8.ToArray());
        var service = new SafeUploadService(new RecordingStorageProvider());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SaveImageAsync("users/test/avatar", content, content.Length));
    }

    [Fact]
    public async Task SaveImageAsync_uses_signature_derived_extension_and_content_type()
    {
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var content = new MemoryStream(png);
        var storage = new RecordingStorageProvider();
        var service = new SafeUploadService(storage);

        var result = await service.SaveImageAsync("users/test/avatar", content, content.Length);

        Assert.EndsWith(".png", result.Key, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("image/png", storage.ContentType);
        Assert.Equal(result.Key, storage.Key);
    }

    private sealed class RecordingStorageProvider : IStorageProvider
    {
        public string? Key { get; private set; }
        public string? ContentType { get; private set; }

        public async Task<StorageResult> SaveAsync(string key, Stream content, string contentType)
        {
            using var sink = new MemoryStream();
            await content.CopyToAsync(sink);
            Key = key;
            ContentType = contentType;
            return new StorageResult(key, $"/uploads/{key}", sink.Length);
        }

        public Task DeleteAsync(string key) => Task.CompletedTask;
        public string GetUrl(string key) => $"/uploads/{key}";
        public string? TryGetKeyFromUrl(string? url) => null;
    }
}
