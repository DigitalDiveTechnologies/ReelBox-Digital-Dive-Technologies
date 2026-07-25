using SocialReelSaver.Application.Media.DTOs;
using SocialReelSaver.Domain.Entities;

namespace SocialReelSaver.Application.Media.Mappings;

public static class MediaMappings
{
    public static MediaResponse ToResponse(this MediaItem item, string? source = null) =>
        new(
            item.Id,
            item.Platform.ToString().ToLowerInvariant(),
            item.Status.ToString().ToLowerInvariant(),
            item.OriginalUrl,
            item.NormalizedUrl,
            item.Title,
            item.ThumbnailStorageKey,
            item.MediaStorageKey,
            item.MimeType,
            item.FileSizeBytes,
            item.DurationMs,
            item.ProgressPercent,
            item.CreatedAt,
            item.DownloadStartedAt,
            item.DownloadedAt,
            item.UpdatedAt,
            item.ErrorCode,
            item.ErrorMessage,
            item.RetryCount,
            source);

    public static MediaListResponse ToListResponse(
        this IReadOnlyList<MediaItem> items,
        int page,
        int pageSize,
        int totalCount) =>
        new(
            items.Select(i => i.ToResponse()).ToList(),
            page,
            pageSize,
            totalCount,
            pageSize <= 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize));
}
