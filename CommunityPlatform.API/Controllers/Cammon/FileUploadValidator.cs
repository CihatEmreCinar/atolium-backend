namespace CommunityPlatform.API.Common;

/// <summary>
/// Yüklenen dosyanın diske hangi uzantıyla yazılacağını, istemcinin gönderdiği
/// dosya adından DEĞİL, sunucunun onayladığı Content-Type'tan türetir.
/// Bu, dosya adı ile içerik-tipi arasındaki uyumsuzluğun (örn. Content-Type:
/// image/jpeg + filename: evil.html) stored XSS'e dönüşmesini engeller.
/// </summary>
public static class FileUploadValidator
{
    private static readonly Dictionary<string, string> ImageExtensions = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"]  = ".png",
        ["image/webp"] = ".webp",
    };

    private static readonly Dictionary<string, string> ImageAndVideoExtensions =
        new(ImageExtensions) { ["video/mp4"] = ".mp4" };

    public static bool TryGetSafeExtension(string? contentType, bool allowVideo, out string extension)
    {
        var map = allowVideo ? ImageAndVideoExtensions : ImageExtensions;
        if (contentType != null && map.TryGetValue(contentType, out var ext))
        {
            extension = ext;
            return true;
        }
        extension = string.Empty;
        return false;
    }
}