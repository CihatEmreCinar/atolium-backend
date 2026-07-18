using System.Security.Cryptography;
using CommunityPlatform.Application.DTOs.Media;
using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Domain.Entities;
using CommunityPlatform.Domain.Enums;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace MediaWorker.Services;

public class MediaProcessor(
    AppDbContext db,
    IMediaObjectStore objectStore,
    IRabbitMqPublisher publisher,
    ILogger<MediaProcessor> logger)
{
    // Backend variant'ları için sabit kalite — kaynak zaten mobile'da 512 KB
    // hedefine optimize edilmiş, burada dinamik merdivene gerek yok.
    private const int VariantQuality = 80;

    public async Task ProcessAsync(MediaUploadedEvent evt, CancellationToken ct)
    {
        var media = await db.Media.FirstOrDefaultAsync(m => m.Id == evt.MediaId, ct);
        if (media is null)
        {
            logger.LogWarning("Media bulunamadı, atlanıyor: {MediaId}", evt.MediaId);
            return;
        }

        if (media.Status != MediaStatus.Pending)
        {
            logger.LogInformation("Media zaten işlenmiş, atlanıyor: {MediaId} ({Status})", media.Id, media.Status);
            return;
        }

        media.Status = MediaStatus.Processing;
        await db.SaveChangesAsync(ct);

        byte[] bytes;
        await using (var stream = await objectStore.GetAsync(evt.TempObjectKey, ct))
        {
            using var mem = new MemoryStream();
            await stream.CopyToAsync(mem, ct);
            bytes = mem.ToArray();
        }

        // 1) Magic number doğrulaması — Content-Type spoof edilebilir, byte'lar edilemez.
        if (!IsWebp(bytes))
        {
            await FailAsync(media, "Geçersiz dosya: WEBP imzası uyuşmuyor.", ct);
            return;
        }

        var checksum = Convert.ToHexStringLower(SHA256.HashData(bytes));

        // 2) Duplicate kontrolü — aynı kullanıcı aynı dosyayı aynı preset'le tekrar
        // yüklediyse yeniden variant üretmeye gerek yok.
        var duplicate = await db.Media
            .Include(m => m.Variants)
            .FirstOrDefaultAsync(m =>
                m.Checksum == checksum &&
                m.OwnerId == media.OwnerId &&
                m.Preset == media.Preset &&
                m.Status == MediaStatus.Ready &&
                m.Id != media.Id, ct);

        if (duplicate is not null)
        {
            await LinkToDuplicateAsync(media, duplicate, checksum, ct);
            return;
        }

        // 3) Decode — WEBP imzası geçerli ama içerik bozuk olabilir (kalıcı hata,
        // requeue'nun anlamı yok, ayrı yakalanıyor).
        Image image;
        try
        {
            image = Image.Load(new MemoryStream(bytes));
        }
        catch (Exception ex)
        {
            await FailAsync(media, $"Görsel decode edilemedi: {ex.Message}", ct);
            return;
        }

        using (image)
        {
            var originalWidth = image.Width;
            var originalHeight = image.Height;

            // 4) Variant üret — orijinalden büyük genişlik istenmiyor (büyütme yok).
            var candidateWidths = VariantPresets.Widths[media.Preset]
                .Where(w => w <= originalWidth)
                .DefaultIfEmpty(originalWidth)
                .Distinct();

            var variants = new List<MediaVariant>();
            var prefix = PrefixFor(media.Preset);

            foreach (var targetWidth in candidateWidths)
            {
                using var clone = image.Clone(ctx => ctx.Resize(targetWidth, 0));
                using var variantStream = new MemoryStream();
                await clone.SaveAsWebpAsync(variantStream, new WebpEncoder { Quality = VariantQuality }, ct);

                var objectKey = $"{prefix}/{media.Id}/{targetWidth}.webp";
                var size = variantStream.Length;
                variantStream.Position = 0;
                await objectStore.PutAsync(objectKey, variantStream, "image/webp", ct);

                variants.Add(new MediaVariant
                {
                    MediaId = media.Id,
                    Width = targetWidth,
                    Height = clone.Height,
                    ObjectKey = objectKey,
                    FileSize = size,
                });
            }

            media.Width = originalWidth;
            media.Height = originalHeight;
            media.Checksum = checksum;
            media.FileSize = bytes.Length;
            media.ObjectKey = variants.OrderByDescending(v => v.Width).First().ObjectKey;
            media.Status = MediaStatus.Ready;
            media.ProcessedAt = DateTime.UtcNow;

            db.MediaVariants.AddRange(variants);
            await db.SaveChangesAsync(ct);

            await objectStore.DeleteAsync(evt.TempObjectKey, ct);

            await publisher.PublishAsync("media.ready", new MediaReadyEvent(
                media.Id, media.ObjectKey!, originalWidth, originalHeight,
                variants.Select(v => new MediaVariantInfo(v.Width, v.Height, v.ObjectKey, v.FileSize)).ToList()));

            logger.LogInformation("Media işlendi: {MediaId}, {Count} variant üretildi.", media.Id, variants.Count);
        }
    }

    private async Task LinkToDuplicateAsync(Media media, Media duplicate, string checksum, CancellationToken ct)
    {
        media.Checksum = checksum;
        media.ObjectKey = duplicate.ObjectKey;
        media.Width = duplicate.Width;
        media.Height = duplicate.Height;
        media.FileSize = duplicate.FileSize;
        media.Status = MediaStatus.Ready;
        media.ProcessedAt = DateTime.UtcNow;

        var copiedVariants = duplicate.Variants.Select(v => new MediaVariant
        {
            MediaId = media.Id,
            Width = v.Width,
            Height = v.Height,
            ObjectKey = v.ObjectKey, // aynı dosyaya işaret eder, kopyalama yok
            FileSize = v.FileSize,
        }).ToList();

        db.MediaVariants.AddRange(copiedVariants);
        await db.SaveChangesAsync(ct);

        await objectStore.DeleteAsync(media.TempObjectKey, ct);

        await publisher.PublishAsync("media.ready", new MediaReadyEvent(
            media.Id, media.ObjectKey!, media.Width!.Value, media.Height!.Value,
            copiedVariants.Select(v => new MediaVariantInfo(v.Width, v.Height, v.ObjectKey, v.FileSize)).ToList()));

        logger.LogInformation("Duplicate tespit edildi, mevcut dosyaya bağlandı: {MediaId} → {DuplicateId}", media.Id, duplicate.Id);
    }

    private async Task FailAsync(Media media, string reason, CancellationToken ct)
    {
        media.Status = MediaStatus.Failed;
        media.FailureReason = reason;
        media.ProcessedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await objectStore.DeleteAsync(media.TempObjectKey, ct);
        logger.LogWarning("Media işlenemedi: {MediaId} — {Reason}", media.Id, reason);
    }

    private static bool IsWebp(byte[] bytes)
    {
        // RIFF <4 byte size> WEBP — ilk 12 byte
        if (bytes.Length < 12) return false;
        return bytes[0] == 'R' && bytes[1] == 'I' && bytes[2] == 'F' && bytes[3] == 'F'
            && bytes[8] == 'W' && bytes[9] == 'E' && bytes[10] == 'B' && bytes[11] == 'P';
    }

    private static string PrefixFor(MediaPreset preset) => preset switch
    {
        MediaPreset.Avatar => "avatars",
        MediaPreset.Cover => "covers",
        MediaPreset.Post => "posts",
        MediaPreset.Marketplace => "marketplace",
        MediaPreset.Gallery => "gallery",
        _ => "misc",
    };
}