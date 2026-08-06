using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Domain.Entities;

/// <summary>
/// Persistent media record (SRS §12.1 media_items).
/// </summary>
public sealed class MediaItem
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public string OriginalUrl { get; set; } = string.Empty;

    public string? NormalizedUrl { get; set; }

    public MediaPlatform Platform { get; set; }

    public MediaStatus Status { get; set; }

    public string? Title { get; set; }

    /// <summary>Caption/description text from the provider (yt-dlp description, etc.).</summary>
    public string? Description { get; set; }

    /// <summary>Uploader / channel / creator username when available.</summary>
    public string? CreatorUsername { get; set; }

    /// <summary>
    /// Extra textual metadata for offline categorization (tags, categories, track, filename tokens, etc.).
    /// </summary>
    public string? MetadataText { get; set; }

    /// <summary>Category label (nullable until background categorization finishes).</summary>
    public string? Category { get; set; }

    /// <summary>Classifier confidence in [0, 1].</summary>
    public double? CategoryConfidence { get; set; }

    /// <summary>Always <c>KeywordEngine</c> (offline deterministic classifier).</summary>
    public string? ClassificationSource { get; set; }

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

    /// <summary>
    /// Optional worker backoff schedule (SRS §12.2 status / next_retry_at index).
    /// </summary>
    public DateTimeOffset? NextRetryAt { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public int RetryCount { get; set; }
}
