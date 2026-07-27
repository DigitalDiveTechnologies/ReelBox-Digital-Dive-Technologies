using SocialReelSaver.Domain.Entities;

namespace SocialReelSaver.Application.Abstractions.Playback;

/// <summary>
/// Builds short-lived signed URLs for thumbnail objects (SRS FR-010).
/// </summary>
public interface IMediaThumbnailUrlService
{
    Task<string?> CreateThumbnailUrlAsync(
        MediaItem item,
        CancellationToken cancellationToken = default);
}
