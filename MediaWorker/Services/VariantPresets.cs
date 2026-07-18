using CommunityPlatform.Domain.Enums;

namespace MediaWorker.Services;

public static class VariantPresets
{
    public static readonly IReadOnlyDictionary<MediaPreset, int[]> Widths = new Dictionary<MediaPreset, int[]>
    {
        [MediaPreset.Avatar]      = [64, 128, 256, 512],
        [MediaPreset.Cover]       = [640, 1280, 1920],
        [MediaPreset.Post]        = [320, 640, 1080, 1920],
        [MediaPreset.Marketplace] = [640, 1080, 2048],
        [MediaPreset.Gallery]     = [320, 640, 1080, 1920],
    };
}