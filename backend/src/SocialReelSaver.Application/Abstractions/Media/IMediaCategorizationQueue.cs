namespace SocialReelSaver.Application.Abstractions.Media;

/// <summary>Queues completed media for background AI categorization.</summary>
public interface IMediaCategorizationQueue
{
    ValueTask EnqueueAsync(Guid mediaId, CancellationToken cancellationToken = default);

    ValueTask<Guid?> DequeueAsync(CancellationToken cancellationToken = default);
}
