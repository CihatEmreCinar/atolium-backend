namespace CommunityPlatform.Domain.Enums;

public enum MediaStatus
{
    Pending,    // Temp'e yüklendi, worker henüz almadı
    Processing, // Worker aldı, variant üretimi sürüyor
    Ready,      // Variant'lar üretildi, final MinIO'da
    Failed      // Worker işleyemedi (bozuk dosya, magic number uyuşmazlığı vb.)
}