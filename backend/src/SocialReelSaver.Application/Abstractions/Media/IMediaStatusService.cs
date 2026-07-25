using SocialReelSaver.Domain.Entities;
using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Application.Abstractions.Media;

public interface IMediaStatusService
{
    void Transition(MediaItem item, MediaStatus toStatus);

    Task MarkDownloadingAsync(MediaItem item, CancellationToken cancellationToken = default);

    Task MarkProcessingAsync(MediaItem item, CancellationToken cancellationToken = default);

    Task MarkValidatingAsync(MediaItem item, CancellationToken cancellationToken = default);

    Task MarkThumbnailAsync(MediaItem item, CancellationToken cancellationToken = default);

    Task MarkUploadingAsync(MediaItem item, CancellationToken cancellationToken = default);

    Task MarkCompletedAsync(
        MediaItem item,
        string? mediaStorageKey,
        string? thumbnailStorageKey,
        string? mimeType,
        long? fileSizeBytes = null,
        long? durationMs = null,
        string? title = null,
        CancellationToken cancellationToken = default);

    Task MarkFailedAsync(
        MediaItem item,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken = default);

    Task MarkQueuedAsync(MediaItem item, CancellationToken cancellationToken = default);

    Task UpdateProgressAsync(
        MediaItem item,
        short progressPercent,
        CancellationToken cancellationToken = default);
}
