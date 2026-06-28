using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CommunityPlatform.Infrastructure.BackgroundJobs;

/// <summary>
/// Her 5 dakikada bir son 72 saatteki postların engagement score'unu günceller.
/// Formül: (likes×1 + comments×2.5 + shares×4 + views×0.1) × time_decay
/// time_decay = 1 / (1 + hours_since / 24)^1.5
///
/// Tüm postları güncellemek yerine sadece aktif postları (son 72 saat) günceller.
/// Daha eski postlar zaten düşük score'a sahip olduğundan sıralamayı etkilemez.
/// </summary>
public class EngagementScoreJob(
    IServiceScopeFactory scopeFactory,
    ILogger<EngagementScoreJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ActiveWindow = TimeSpan.FromHours(72);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EngagementScoreJob başlatıldı.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "EngagementScoreJob hatası.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoff = DateTime.UtcNow - ActiveWindow;

        // Son 72 saatteki postları çek — counter'lar trigger ile güncel
        var posts = await db.Posts
            .Where(p => p.PublishedAt >= cutoff)
            .ToListAsync(ct);

        if (posts.Count == 0) return;

        var now = DateTime.UtcNow;

        foreach (var post in posts)
        {
            var hoursSince = (now - post.PublishedAt).TotalHours;
            var decay = 1.0 / Math.Pow(1.0 + hoursSince / 24.0, 1.5);

            post.EngagementScore =
                (post.LikeCount * 1.0 +
                 post.CommentCount * 2.5 +
                 post.ShareCount * 4.0 +
                 post.ViewCount * 0.1)
                * decay;
        }

        // Toplu güncelleme — tek SaveChanges
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "EngagementScoreJob: {Count} post güncellendi.",
            posts.Count);
    }
}
