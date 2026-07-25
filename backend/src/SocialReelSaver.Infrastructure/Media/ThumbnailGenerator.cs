using SocialReelSaver.Application.Abstractions.Media;

namespace SocialReelSaver.Infrastructure.Media;

/// <summary>
/// Thumbnail generation architecture only — FFmpeg extraction deferred.
/// </summary>
public sealed class ThumbnailGenerator : IThumbnailService
{
    public Task<ThumbnailResult> GenerateAsync(
        string mediaLocalPath,
        Guid mediaId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // TODO: Extract thumbnail frame with FFmpeg (SRS FR-010).
        _ = mediaLocalPath;
        _ = mediaId;
        return Task.FromResult(
            ThumbnailResult.Skipped("Thumbnail generation via FFmpeg is not implemented yet."));
    }
}
