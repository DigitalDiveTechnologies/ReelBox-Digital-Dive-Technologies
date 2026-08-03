using SocialReelSaver.Application.Abstractions.Admin;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Abstractions.Queue;
using SocialReelSaver.Application.Admin.DTOs;
using SocialReelSaver.Application.Common;
using SocialReelSaver.Application.Common.Exceptions;
using SocialReelSaver.Application.Media.Jobs;
using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Application.Admin.UseCases;

public sealed class ListJobsAdminUseCase(IMediaRepository media)
{
    public async Task<(PagedResult<AdminMediaListItem> Page, JobStatusCountsResponse Counts)> HandleAsync(
        int page, int pageSize, string? statusGroup, string? search, MediaPlatform? platform,
        Guid? userId, string? sortBy, string? sortDir, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PagedResult<AdminMediaListItem>.Normalize(page, pageSize);
        var statuses = AdminJobStatusGroups.Resolve(statusGroup);
        var result = await media.ListAdminAsync(page, pageSize, search, null, platform, userId, statuses, sortBy, sortDir, cancellationToken);
        var counts = await media.StatusCountsAsync(cancellationToken);
        var pageResult = new PagedResult<AdminMediaListItem>(
            result.Items.Select(ListMediaAdminUseCase.MapList).ToList(), page, pageSize, result.TotalCount);
        var summary = new JobStatusCountsResponse(
            counts.GetValueOrDefault(MediaStatus.Queued) + counts.GetValueOrDefault(MediaStatus.Preparing),
            counts.GetValueOrDefault(MediaStatus.Downloading) + counts.GetValueOrDefault(MediaStatus.Processing),
            counts.GetValueOrDefault(MediaStatus.Completed),
            counts.GetValueOrDefault(MediaStatus.Failed),
            counts.Values.Sum());
        return (pageResult, summary);
    }
}

public sealed class RetryJobAdminUseCase(RetryMediaAdminUseCase retry)
{
    public Task<AdminMediaDetailResponse> HandleAsync(
        Guid id, Guid adminId, string adminEmail, string? ip, string? correlationId, CancellationToken cancellationToken = default) =>
        retry.HandleAsync(id, adminId, adminEmail, ip, correlationId, cancellationToken);
}

public sealed class CancelJobAdminUseCase(IMediaRepository media, IAuditLogWriter audit)
{
    public async Task HandleAsync(Guid id, Guid adminId, string adminEmail, string? ip, string? correlationId, CancellationToken cancellationToken = default)
    {
        var item = await media.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Job not found.");
        if (item.Status is MediaStatus.Completed or MediaStatus.Failed)
            throw new BadRequestException("Completed or failed jobs cannot be cancelled.");

        var old = item.Status.ToString();
        item.Status = MediaStatus.Failed;
        item.ErrorCode = "CANCELLED_BY_ADMIN";
        item.ErrorMessage = "Cancelled by administrator.";
        item.NextRetryAt = null;
        item.ProgressPercent = null;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await media.UpdateAsync(item, cancellationToken);
        await media.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync(adminId, adminEmail, "job.cancelled", "MediaItem", id.ToString(),
            new { status = old }, new { status = item.Status.ToString(), item.ErrorCode }, ip, correlationId, cancellationToken);
    }
}

public sealed class RequeueJobAdminUseCase(IMediaRepository media, IMediaJobPublisher jobs, IAuditLogWriter audit)
{
    public async Task HandleAsync(Guid id, Guid adminId, string adminEmail, string? ip, string? correlationId, CancellationToken cancellationToken = default)
    {
        var item = await media.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Job not found.");
        if (item.Status == MediaStatus.Completed)
            throw new BadRequestException("Completed jobs cannot be requeued.");

        var old = item.Status.ToString();
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
        await audit.WriteAsync(adminId, adminEmail, "job.requeued", "MediaItem", id.ToString(),
            new { status = old }, new { status = item.Status.ToString() }, ip, correlationId, cancellationToken);
    }
}
