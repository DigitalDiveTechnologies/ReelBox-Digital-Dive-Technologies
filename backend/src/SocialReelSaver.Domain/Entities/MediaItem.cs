using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Domain.Entities;

/// <summary>
/// Persistent media record (SRS §12.1 media_items).
/// </summary>
public sealed class MediaItem
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string OriginalUrl { get; set; } = string.Empty;

    public string? NormalizedUrl { get; set; }

    public MediaPlatform Platform { get; set; }

    public MediaStatus Status { get; set; }

    public string? Title { get; set; }

    public string? ThumbnailStorageKey { get; set; }

    public string? MediaStorageKey { get; set; }

    public string? MimeType { get; set; }

    public long? FileSizeBytes { get; set; }

    public long? DurationMs { get; set; }

    public short? ProgressPercent { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? DownloadStartedAt { get; set; }

    public DateTimeOffset? DownloadedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public int RetryCount { get; set; }
}
