namespace CommunityPlatform.Application.DTOs.Media;

// API → Worker. Preset bilinçli olarak string (enum JSON serileştirme
// uyumsuzluğu iki bağımsız proje arasında sorun çıkarmasın diye).
public record MediaUploadedEvent(
    Guid MediaId,
    string Bucket,
    string TempObjectKey,
    string Preset
);