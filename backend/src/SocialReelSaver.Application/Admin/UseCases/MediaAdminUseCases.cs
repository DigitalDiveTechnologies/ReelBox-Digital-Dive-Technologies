using Microsoft.Extensions.Logging;
using SocialReelSaver.Application.Abstractions.Admin;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Abstractions.Playback;
using SocialReelSaver.Application.Abstractions.Queue;
using SocialReelSaver.Application.Abstractions.Storage;
using SocialReelSaver.Application.Admin.DTOs;
using SocialReelSaver.Application.Common;
using SocialReelSaver.Application.Common.Exceptions;
using SocialReelSaver.Application.Media.Jobs;
using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Application.Admin.UseCases;

public sealed class ListMediaAdminUseCase(IMediaRepository media)
{
    public async Task<PagedResult<AdminMediaListItem>> HandleAsync(
        int page, int pageSize, string? search, MediaStatus? status, MediaPlatform? platform,
        Guid? userId, string? sortBy, string? sortDir, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PagedResult<AdminMediaListItem>.Normalize(page, pageSize);
        var result = await media.ListAdminAsync(page, pageSize, search, status, platform, userId, null, sortBy, sortDir, cancellationToken);
        return new(result.Items.Select(MapList).ToList(), page, pageSize, result.TotalCount);
    }

    internal static AdminMediaListItem MapList(Domain.Entities.MediaItem x) =>
        new(x.Id, x.UserId, x.User?.Email, x.Platform.ToString(), x.Status.ToString(),
            x.OriginalUrl, x.Title, x.FileSizeBytes, x.RetryCount, x.CreatedAt, x.UpdatedAt, x.ErrorCode);
}

public sealed class GetMediaAdminUseCase(IMediaRepository media)
{
    public async Task<AdminMediaDetailResponse> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var x = await media.GetByIdWithUserAsync(id, cancellationToken) ?? throw new NotFoundException("Media item not found.");
        return new(x.Id, x.UserId, x.User?.Email, x.Platform.ToString(), x.Status.ToString(), x.OriginalUrl,
            x.NormalizedUrl, x.Title, x.ThumbnailStorageKey, x.MediaStorageKey, x.MimeType, x.FileSizeBytes,
            x.DurationMs, x.ProgressPercent, x.RetryCount, x.CreatedAt, x.DownloadStartedAt, x.DownloadedAt,
            x.UpdatedAt, x.NextRetryAt, x.ErrorCode, x.ErrorMessage);
    }
}

public sealed class DeleteMediaAdminUseCase(IMediaRepository media, IObjectStorageService storage, IAuditLogWriter audit, ILogger<DeleteMediaAdminUseCase> logger)
{
    public async Task HandleAsync(Guid id, Guid adminId, string adminEmail, string? ip, string? correlationId, CancellationToken cancellationToken = default)
    {
        var item = await media.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Media item not found.");
        await DeleteObjectAsync(item.MediaStorageKey, cancellationToken);
        await DeleteObjectAsync(item.ThumbnailStorageKey, cancellationToken);
        await media.DeleteAsync(item, cancellationToken);
        await media.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync(adminId, adminEmail, "media.deleted", "MediaItem", id.ToString(),
            new { item.UserId, status = item.Status.ToString() }, null, ip, correlationId, cancellationToken);
    }

    private async Task DeleteObjectAsync(string? key, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        var result = await storage.DeleteAsync(key, cancellationToken);
        if (!result.Success && !result.IsNotImplemented)
            logger.LogWarning("Failed to delete storage object {Key}: {Code} {Message}", key, result.ErrorCode, result.ErrorMessage);
    }
}

public sealed class RetryMediaAdminUseCase(IMediaRepository media, IMediaJobPublisher jobs, IAuditLogWriter audit)
{
    private static readonly MediaStatus[] Retryable =
        [MediaStatus.Failed, MediaStatus.Preparing, MediaStatus.Queued];

    public async Task<AdminMediaDetailResponse> HandleAsync(
        Guid id, Guid adminId, string adminEmail, string? ip, string? correlationId, CancellationToken cancellationToken = default)
    {
        var item = await media.GetByIdWithUserAsync(id, cancellationToken) ?? throw new NotFoundException("Media item not found.");
        if (item.Status == MediaStatus.Completed)
            throw new BadRequestException("Completed media cannot be retried.");
        if (!Retryable.Contains(item.Status) && item.Status is not (MediaStatus.Downloading or MediaStatus.Processing))
            throw new BadRequestException("Media status is not retryable.");

        // Allow Failed, Preparing, Queued, and stuck Downloading/Processing
        if (item.Status is MediaStatus.Downloading or MediaStatus.Processing
            && item.UpdatedAt > DateTimeOffset.UtcNow.AddMinutes(-10))
            throw new BadRequestException("Active download is recent; wait before retrying.");

        var oldStatus = item.Status.ToString();
        item.Status = MediaStatus.Queued;
        item.RetryCount += 1;
        item.ErrorCode = null;
        item.ErrorMessage = null;
        item.ProgressPercent = null;
        item.DownloadStartedAt = null;
        item.NextRetryAt = null;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await media.UpdateAsync(item, cancellationToken);
        await media.SaveChangesAsync(cancellationToken);
        await jobs.PublishDownloadJobAsync(new MediaDownloadJob
        {
            JobId = Guid.NewGuid(),
            MediaId = item.Id,
            UserId = item.UserId,
            Platform = item.Platform,
            OriginalUrl = item.OriginalUrl,
            Attempt = item.RetryCount,
            CreatedAt = DateTimeOffset.UtcNow,
        }, cancellationToken);
        await audit.WriteAsync(adminId, adminEmail, "media.retried", "MediaItem", id.ToString(),
            new { status = oldStatus }, new { status = item.Status.ToString() }, ip, correlationId, cancellationToken);

        return new(item.Id, item.UserId, item.User?.Email, item.Platform.ToString(), item.Status.ToString(), item.OriginalUrl,
            item.NormalizedUrl, item.Title, item.ThumbnailStorageKey, item.MediaStorageKey, item.MimeType, item.FileSizeBytes,
            item.DurationMs, item.ProgressPercent, item.RetryCount, item.CreatedAt, item.DownloadStartedAt, item.DownloadedAt,
            item.UpdatedAt, item.NextRetryAt, item.ErrorCode, item.ErrorMessage);
    }
}

public sealed class GetMediaPlaybackAdminUseCase(IMediaRepository media, IPlaybackUrlService playback)
{
    public async Task<PlaybackMetadata> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await media.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Media item not found.");
        // Sign as media owner so local HMAC URLs validate against uid.
        return await playback.CreateAsync(item, item.UserId, cancellationToken);
    }
}
