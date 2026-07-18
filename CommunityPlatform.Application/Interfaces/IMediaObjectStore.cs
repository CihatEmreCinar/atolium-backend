namespace CommunityPlatform.Application.Interfaces;

/// <summary>
/// Yeni unified medya pipeline'ının MinIO soyutlaması. Mevcut IStorageProvider
/// (LocalStorageProvider, disk tabanlı) legacy PostMedia/SpaceListingPhoto akışı
/// için olduğu gibi kalıyor — bu arayüz yalnızca Media/MediaVariant sistemi
/// içindir, ikisi birbirine karışmaz.
/// </summary>
public interface IMediaObjectStore
{
    Task<long> PutAsync(string objectKey, Stream content, string contentType, CancellationToken ct = default);

    Task<Stream> GetAsync(string objectKey, CancellationToken ct = default);

    Task DeleteAsync(string objectKey, CancellationToken ct = default);

    /// <summary>Bucket yoksa oluşturur. Uygulama/worker başlangıcında bir kez çağrılır.</summary>
    Task EnsureBucketExistsAsync(CancellationToken ct = default);
}