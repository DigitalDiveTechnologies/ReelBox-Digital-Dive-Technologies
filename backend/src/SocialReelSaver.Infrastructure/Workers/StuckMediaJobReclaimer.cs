using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialReelSaver.Application.Abstractions.Media;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Abstractions.Queue;
using SocialReelSaver.Application.Media.Errors;
using SocialReelSaver.Application.Media.Jobs;
using SocialReelSaver.Domain.Enums;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Infrastructure.Workers;

/// <summary>
/// Recovers MediaItems left in Downloading/Processing after Worker crash (Redis RPOP already consumed).
/// </summary>
public sealed class StuckMediaJobReclaimer
{
    public const string OrphanErrorCode = "WORKER_CRASH_ORPHAN";

    private readonly IMediaRepository _media;
    private readonly IMediaStatusService _status;
    private readonly IMediaJobPublisher _publisher;
    private readonly IRetryPolicy _retryPolicy;
    private readonly WorkerOptions _options;
    private readonly ILogger<StuckMediaJobReclaimer> _logger;

    public StuckMediaJobReclaimer(
        IMediaRepository media,
        IMediaStatusService status,
        IMediaJobPublisher publisher,
        IRetryPolicy retryPolicy,
        IOptions<WorkerOptions> options,
        ILogger<StuckMediaJobReclaimer> logger)
    {
        _media = media;
        _status = status;
        _publisher = publisher;
        _retryPolicy = retryPolicy;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> ReclaimAsync(CancellationToken cancellationToken = default)
    {
        var timeoutMinutes = Math.Clamp(_options.StuckJobTimeoutMinutes, 1, 24 * 60);
        var staleBefore = DateTimeOffset.UtcNow.AddMinutes(-timeoutMinutes);
        var stuck = await _media.ListStaleActiveAsync(staleBefore, take: 100, cancellationToken);
        if (stuck.Count == 0)
        {
            return 0;
        }

        var reclaimed = 0;
        foreach (var item in stuck)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Re-read to avoid racing a live worker that just finished.
            var current = await _media.GetByIdAsync(item.Id, cancellationToken);
            if (current is null)
            {
                continue;
            }

            if (current.Status is not (MediaStatus.Downloading or MediaStatus.Processing))
            {
                continue;
            }

            var activityAt = current.DownloadStartedAt ?? current.UpdatedAt;
            if (activityAt >= staleBefore)
            {
                continue;
            }

            if (!_retryPolicy.CanRetry(current.RetryCount, OrphanErrorCode))
            {
                await _status.MarkFailedAsync(
                    current,
                    SrsMediaErrorCodes.ToPublic(OrphanErrorCode),
                    "Download abandoned after worker crash; max retries exceeded.",
                    cancellationToken);

                _logger.LogWarning(
                    "Orphan media {MediaId} marked Failed after {RetryCount} retries",
                    current.Id,
                    current.RetryCount);
                continue;
            }

            current.RetryCount += 1;
            current.ErrorCode = null;
            current.ErrorMessage = null;
            current.ProgressPercent = null;
            current.DownloadStartedAt = null;
            current.NextRetryAt = null;
            await _status.MarkQueuedAsync(current, nextRetryAt: null, cancellationToken);

            await _publisher.PublishDownloadJobAsync(
                new MediaDownloadJob
                {
                    JobId = Guid.NewGuid(),
                    MediaId = current.Id,
                    UserId = current.UserId,
                    Platform = current.Platform,
                    OriginalUrl = current.OriginalUrl,
                    Attempt = current.RetryCount,
                    CreatedAt = DateTimeOffset.UtcNow,
                },
                cancellationToken);

            reclaimed++;
            _logger.LogInformation(
                "Reclaimed orphan media {MediaId} (was {Status}); republished attempt {Attempt}",
                current.Id,
                item.Status,
                current.RetryCount);
        }

        return reclaimed;
    }
}
