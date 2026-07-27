using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Application.Media.DTOs;

public sealed record CreateMediaRequest(string Url, string? Source);

public sealed record MediaResponse(
    Guid Id,
    string Platform,
    string Status,
    string OriginalUrl,
    string? NormalizedUrl,
    string? Title,
    string? ThumbnailStorageKey,
    string? MediaStorageKey,
    string? MimeType,
    long? FileSizeBytes,
    long? DurationMs,
    short? ProgressPercent,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DownloadStartedAt,
    DateTimeOffset? DownloadedAt,
    DateTimeOffset UpdatedAt,
    string? ErrorCode,
    string? ErrorMessage,
    int RetryCount,
    string? Source,
    string? ThumbnailUrl = null);

public sealed record MediaListResponse(
    IReadOnlyList<MediaResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record PlaybackResponse(
    Guid MediaId,
    string Status,
    string? MediaStorageKey,
    string? ThumbnailStorageKey,
    string? MimeType,
    string? PlaybackUrl,
    string Delivery,
    DateTimeOffset? ExpiresAt,
    string? ThumbnailUrl = null);
