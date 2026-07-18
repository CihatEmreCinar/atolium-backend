namespace CommunityPlatform.Application.DTOs.Media;

// Worker → şimdilik dinleyen yok; ileride yeni bir feature bağlanınca kullanılacak.
public record MediaReadyEvent(
    Guid MediaId,
    string ObjectKey,
    int Width,
    int Height,
    IReadOnlyList<MediaVariantInfo> Variants
);

public record MediaVariantInfo(int Width, int Height, string ObjectKey, long FileSize);