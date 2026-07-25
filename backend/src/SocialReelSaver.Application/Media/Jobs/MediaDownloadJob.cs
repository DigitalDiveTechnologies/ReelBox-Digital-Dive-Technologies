using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Application.Media.Jobs;

/// <summary>
/// Queue payload for asynchronous media download work (SRS FR-006 / §11).
/// </summary>
public sealed class MediaDownloadJob
{
    public Guid JobId { get; init; } = Guid.NewGuid();

    public Guid MediaId { get; init; }

    public Guid UserId { get; init; }

    public MediaPlatform Platform { get; init; }

    public string OriginalUrl { get; init; } = string.Empty;

    public int Attempt { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Optional delay gate for exponential backoff retries.
    /// </summary>
    public DateTimeOffset? NotBefore { get; init; }
}
