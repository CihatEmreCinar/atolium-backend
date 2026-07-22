using CommunityPlatform.Application.Interfaces;

namespace CommunityPlatform.Infrastructure.Services;

/// <summary>
/// Validates uploaded content before it is persisted. Client supplied file names and
/// Content-Type values are never used to select the stored file extension or MIME type.
/// </summary>
public sealed class SafeUploadService(IStorageProvider storage)
{
    private const int ImageMaxBytes = 10 * 1024 * 1024;
    private const int PostMediaMaxBytes = 50 * 1024 * 1024;

    public async Task<StorageResult> SaveImageAsync(string keyPrefix, Stream content, long length, CancellationToken cancellationToken = default)
    {
        EnsureLength(length, ImageMaxBytes);
        var fileType = await DetectAsync(content, allowVideo: false, cancellationToken);
        return await SaveAsync(keyPrefix, content, fileType, cancellationToken);
    }

    public async Task<StorageResult> SavePostMediaAsync(string keyPrefix, Stream content, long length, CancellationToken cancellationToken = default)
    {
        EnsureLength(length, PostMediaMaxBytes);
        var fileType = await DetectAsync(content, allowVideo: true, cancellationToken);
        if (fileType.ContentType is not ("image/webp" or "video/mp4"))
            throw new ArgumentException("Post görselleri geçerli WEBP, videolar geçerli MP4 olmalıdır.");
        return await SaveAsync(keyPrefix, content, fileType, cancellationToken);
    }

    public async Task ValidateWebpAsync(Stream content, long length, CancellationToken cancellationToken = default)
    {
        EnsureLength(length, 2 * 1024 * 1024);
        var fileType = await DetectAsync(content, allowVideo: false, cancellationToken);
        if (fileType.ContentType != "image/webp")
            throw new ArgumentException("Yalnızca geçerli WEBP dosyaları kabul edilir.");
    }

    private async Task<StorageResult> SaveAsync(string keyPrefix, Stream content, DetectedFileType fileType, CancellationToken cancellationToken)
    {
        if (keyPrefix.Contains("..", StringComparison.Ordinal) || keyPrefix.StartsWith('/'))
            throw new ArgumentException("Geçersiz dosya yolu.");

        var key = $"{keyPrefix.TrimEnd('/')}/{Guid.NewGuid():N}{fileType.Extension}";
        return await storage.SaveAsync(key, content, fileType.ContentType);
    }

    private static void EnsureLength(long length, long maxBytes)
    {
        if (length <= 0)
            throw new ArgumentException("Dosya boş olamaz.");

        if (length > maxBytes)
            throw new ArgumentException($"Dosya boyutu {maxBytes / (1024 * 1024)} MB'ı geçemez.");
    }

    private static async Task<DetectedFileType> DetectAsync(Stream content, bool allowVideo, CancellationToken cancellationToken)
    {
        if (!content.CanSeek)
            throw new ArgumentException("Yükleme akışı doğrulama için seekable olmalıdır.");

        var originalPosition = content.Position;
        try
        {
            var header = new byte[16];
            var read = 0;
            while (read < header.Length)
            {
                var count = await content.ReadAsync(header.AsMemory(read, header.Length - read), cancellationToken);
                if (count == 0) break;
                read += count;
            }

            if (IsJpeg(header, read)) return new("image/jpeg", ".jpg");
            if (IsPng(header, read)) return new("image/png", ".png");
            if (IsWebp(header, read)) return new("image/webp", ".webp");
            if (allowVideo && IsMp4(header, read)) return new("video/mp4", ".mp4");

            throw new ArgumentException("Dosya içeriği desteklenen bir görsel veya video formatı değil.");
        }
        finally
        {
            content.Position = originalPosition;
        }
    }

    private static bool IsJpeg(byte[] bytes, int length) => length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;
    private static bool IsPng(byte[] bytes, int length) => length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 && bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A;
    private static bool IsWebp(byte[] bytes, int length) => length >= 12 && bytes[0] == 'R' && bytes[1] == 'I' && bytes[2] == 'F' && bytes[3] == 'F' && bytes[8] == 'W' && bytes[9] == 'E' && bytes[10] == 'B' && bytes[11] == 'P';
    private static bool IsMp4(byte[] bytes, int length) => length >= 12 && bytes[4] == 'f' && bytes[5] == 't' && bytes[6] == 'y' && bytes[7] == 'p';

    private sealed record DetectedFileType(string ContentType, string Extension);
}
