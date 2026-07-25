using SocialReelSaver.Application.Media.Jobs;

namespace SocialReelSaver.Application.Abstractions.Queue;

/// <summary>
/// Queue abstraction for media download jobs. Redis is the MVP implementation (SRS §18).
/// </summary>
public interface IMediaJobQueue
{
    Task PublishAsync(MediaDownloadJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dequeues the next available job, or returns null when idle/timeout.
    /// </summary>
    Task<MediaDownloadJob?> DequeueAsync(CancellationToken cancellationToken = default);
}

public interface IMediaJobPublisher
{
    Task PublishDownloadJobAsync(MediaDownloadJob job, CancellationToken cancellationToken = default);
}

public interface IMediaJobConsumer
{
    Task<MediaDownloadJob?> ConsumeAsync(CancellationToken cancellationToken = default);
}
