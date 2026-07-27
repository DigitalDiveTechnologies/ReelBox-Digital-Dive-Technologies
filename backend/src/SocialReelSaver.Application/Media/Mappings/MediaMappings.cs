using SocialReelSaver.Application.Media.DTOs;
using SocialReelSaver.Domain.Entities;

namespace SocialReelSaver.Application.Media.Mappings;

public static class MediaMappings
{
    public static MediaResponse ToResponse(
        this MediaItem item,
        string? source = null,
        string? thumbnailUrl = null) =>
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
            source,
            thumbnailUrl);

    public static MediaListResponse ToListResponse(
        this IReadOnlyList<MediaItem> items,
        int page,
        int pageSize,
        int totalCount,
        IReadOnlyDictionary<Guid, string?>? thumbnailUrls = null) =>
        new(
            items.Select(i =>
            {
                string? thumbUrl = null;
                thumbnailUrls?.TryGetValue(i.Id, out thumbUrl);
                return i.ToResponse(thumbnailUrl: thumbUrl);
            }).ToList(),
            page,
            pageSize,
            totalCount,
            pageSize <= 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize));
}
