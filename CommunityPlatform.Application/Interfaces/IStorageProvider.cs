namespace CommunityPlatform.Application.Interfaces;

public interface IStorageProvider
{
    /// <summary>
    /// Dosyayı storage'a kaydeder. Key: "posts/{postId}/{filename}"
    /// Döndürdüğü string serve URL'idir (local: relative, S3: absolute).
    /// </summary>
    Task<StorageResult> SaveAsync(string key, Stream content, string contentType);

    /// <summary>
    /// Dosyayı siler.
    /// </summary>
    Task DeleteAsync(string key);

    /// <summary>
    /// Serve edilebilir URL döner. Local'de "/uploads/{key}", S3'te CDN URL.
    /// </summary>
    string GetUrl(string key);
}

public record StorageResult(string Key, string Url, long SizeBytes);
