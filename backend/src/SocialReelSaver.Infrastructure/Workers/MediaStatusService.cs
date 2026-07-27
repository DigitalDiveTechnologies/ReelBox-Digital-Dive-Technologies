using SocialReelSaver.Application.Abstractions.Media;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Media.Errors;
using SocialReelSaver.Application.Media.State;
using SocialReelSaver.Domain.Entities;
using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Infrastructure.Workers;

public sealed class MediaStatusService : IMediaStatusService
{
    private readonly IMediaRepository _media;

    public MediaStatusService(IMediaRepository media)
    {
        _media = media;
    }

    public void Transition(MediaItem item, MediaStatus toStatus)
    {
        MediaStateMachine.EnsureTransition(item.Status, toStatus);
        item.Status = toStatus;
        item.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public async Task MarkDownloadingAsync(MediaItem item, CancellationToken cancellationToken = default)
    {
        if (item.Status == MediaStatus.Failed)
        {
            Transition(item, MediaStatus.Queued);
        }

        Transition(item, MediaStatus.Downloading);
        item.DownloadStartedAt ??= DateTimeOffset.UtcNow;
        item.NextRetryAt = null;
        item.ProgressPercent = 10;
        await PersistAsync(item, cancellationToken);
    }

    public async Task MarkProcessingAsync(MediaItem item, CancellationToken cancellationToken = default)
    {
        EnsureProcessing(item, progressPercent: 40);
        await PersistAsync(item, cancellationToken);
    }

    /// <summary>
    /// Internal validate stage — persisted status remains <see cref="MediaStatus.Processing"/> (SRS §13).
    /// </summary>
    public async Task MarkValidatingAsync(MediaItem item, CancellationToken cancellationToken = default)
    {
        EnsureProcessing(item, progressPercent: 55);
        await PersistAsync(item, cancellationToken);
    }

    /// <summary>
    /// Internal thumbnail stage — persisted status remains <see cref="MediaStatus.Processing"/> (SRS §13).
    /// </summary>
    public async Task MarkThumbnailAsync(MediaItem item, CancellationToken cancellationToken = default)
    {
        EnsureProcessing(item, progressPercent: 70);
        await PersistAsync(item, cancellationToken);
    }

    /// <summary>
    /// Internal upload stage — persisted status remains <see cref="MediaStatus.Processing"/> (SRS §13).
    /// </summary>
    public async Task MarkUploadingAsync(MediaItem item, CancellationToken cancellationToken = default)
    {
        EnsureProcessing(item, progressPercent: 85);
        await PersistAsync(item, cancellationToken);
    }

    public async Task MarkCompletedAsync(
        MediaItem item,
        string? mediaStorageKey,
        string? thumbnailStorageKey,
        string? mimeType,
        long? fileSizeBytes = null,
        long? durationMs = null,
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        Transition(item, MediaStatus.Completed);
        item.MediaStorageKey = mediaStorageKey;
        item.ThumbnailStorageKey = thumbnailStorageKey;
        item.MimeType = mimeType;
        if (fileSizeBytes.HasValue)
        {
            item.FileSizeBytes = fileSizeBytes;
        }

        if (durationMs.HasValue)
        {
            item.DurationMs = durationMs;
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            item.Title = title;
        }

        item.DownloadedAt = DateTimeOffset.UtcNow;
        item.ErrorCode = null;
        item.ErrorMessage = null;
        item.NextRetryAt = null;
        item.ProgressPercent = 100;
        await PersistAsync(item, cancellationToken);
    }

    public async Task MarkFailedAsync(
        MediaItem item,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        Transition(item, MediaStatus.Failed);
        item.ErrorCode = SrsMediaErrorCodes.ToPublic(errorCode);
        item.ErrorMessage = errorMessage;
        item.NextRetryAt = null;
        await PersistAsync(item, cancellationToken);
    }

    public async Task MarkQueuedAsync(
        MediaItem item,
        DateTimeOffset? nextRetryAt = null,
        CancellationToken cancellationToken = default)
    {
        Transition(item, MediaStatus.Queued);
        item.NextRetryAt = nextRetryAt;
        await PersistAsync(item, cancellationToken);
    }

    public async Task UpdateProgressAsync(
        MediaItem item,
        short progressPercent,
        CancellationToken cancellationToken = default)
    {
        item.ProgressPercent = progressPercent;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await PersistAsync(item, cancellationToken);
    }

    private void EnsureProcessing(MediaItem item, short progressPercent)
    {
        if (item.Status == MediaStatus.Downloading)
        {
            Transition(item, MediaStatus.Processing);
        }
        else if (item.Status == MediaStatus.Processing)
        {
            item.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            MediaStateMachine.EnsureTransition(item.Status, MediaStatus.Processing);
            item.Status = MediaStatus.Processing;
            item.UpdatedAt = DateTimeOffset.UtcNow;
        }

        item.ProgressPercent = progressPercent;
    }

    private async Task PersistAsync(MediaItem item, CancellationToken cancellationToken)
    {
        await _media.UpdateAsync(item, cancellationToken);
        await _media.SaveChangesAsync(cancellationToken);
    }
}
