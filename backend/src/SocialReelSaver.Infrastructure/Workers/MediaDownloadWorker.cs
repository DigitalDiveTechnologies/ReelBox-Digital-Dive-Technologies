using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialReelSaver.Application.Abstractions.Media;
using SocialReelSaver.Application.Abstractions.Queue;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Infrastructure.Workers;

/// <summary>
/// Background consumer for media download jobs (SRS §11 / §19).
/// Also drains categorization jobs after downloads complete (non-blocking)
/// and reclaims stale Downloading/Processing rows after Worker crashes.
/// </summary>
public sealed class MediaDownloadWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<WorkerOptions> _options;
    private readonly ILogger<MediaDownloadWorker> _logger;
    private DateTimeOffset _nextOrphanScanUtc = DateTimeOffset.MinValue;

    public MediaDownloadWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<WorkerOptions> options,
        ILogger<MediaDownloadWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Media download worker started. Queue={QueueName}",
            _options.Value.QueueName);

        await TryReclaimOrphansAsync(force: true, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TryReclaimOrphansAsync(force: false, stoppingToken);

                using var scope = _scopeFactory.CreateScope();
                var consumer = scope.ServiceProvider.GetRequiredService<IMediaJobConsumer>();
                var pipeline = scope.ServiceProvider.GetRequiredService<MediaDownloadPipeline>();
                var categorizationQueue = scope.ServiceProvider.GetRequiredService<IMediaCategorizationQueue>();
                var categorizer = scope.ServiceProvider.GetRequiredService<IMediaCategorizationService>();

                var job = await consumer.ConsumeAsync(stoppingToken);
                if (job is not null)
                {
                    _logger.LogInformation(
                        "Processing job {JobId} for media {MediaId} (attempt {Attempt})",
                        job.JobId,
                        job.MediaId,
                        job.Attempt);

                    await pipeline.ExecuteAsync(job, stoppingToken);
                    continue;
                }

                var mediaId = await categorizationQueue.DequeueAsync(stoppingToken);
                if (mediaId is Guid id)
                {
                    try
                    {
                        await categorizer.CategorizeAsync(id, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Categorization failed for media {MediaId}", id);
                    }

                    continue;
                }

                await Task.Delay(_options.Value.PollIntervalMilliseconds, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker loop error");
                await Task.Delay(_options.Value.PollIntervalMilliseconds, stoppingToken);
            }
        }

        _logger.LogInformation("Media download worker stopped");
    }

    private async Task TryReclaimOrphansAsync(bool force, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        if (!force && now < _nextOrphanScanUtc)
        {
            return;
        }

        // Scan at most every half of stuck timeout (min 60s).
        var timeoutMinutes = Math.Clamp(_options.Value.StuckJobTimeoutMinutes, 1, 24 * 60);
        var interval = TimeSpan.FromMinutes(Math.Max(1, timeoutMinutes / 2.0));
        if (interval < TimeSpan.FromSeconds(60))
        {
            interval = TimeSpan.FromSeconds(60);
        }

        _nextOrphanScanUtc = now.Add(interval);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var reclaimer = scope.ServiceProvider.GetRequiredService<StuckMediaJobReclaimer>();
            var count = await reclaimer.ReclaimAsync(cancellationToken);
            if (count > 0)
            {
                _logger.LogInformation("Orphan reclaim republished {Count} job(s)", count);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Orphan reclaim scan failed");
        }
    }
}
