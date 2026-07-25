using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Abstractions.Queue;
using SocialReelSaver.Application.Common.Exceptions;
using SocialReelSaver.Application.Media.DTOs;
using SocialReelSaver.Application.Media.Errors;
using SocialReelSaver.Application.Media.Jobs;
using SocialReelSaver.Application.Media.Mappings;
using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Application.Media.UseCases;

public sealed class RetryMediaUseCase
{
    private readonly IMediaRepository _media;
    private readonly IMediaJobPublisher _jobPublisher;

    public RetryMediaUseCase(IMediaRepository media, IMediaJobPublisher jobPublisher)
    {
        _media = media;
        _jobPublisher = jobPublisher;
    }

    public async Task<MediaResponse> HandleAsync(
        Guid userId,
        Guid mediaId,
        CancellationToken cancellationToken = default)
    {
        var item = await _media.GetByIdForUserAsync(mediaId, userId, cancellationToken);
        if (item is null)
        {
            throw new NotFoundException("Media item not found.");
        }

        if (item.Status == MediaStatus.Completed)
        {
            throw new BadRequestException(
                "Completed media cannot be retried.",
                SrsMediaErrorCodes.Unknown);
        }

        if (item.Status != MediaStatus.Failed)
        {
            throw new BadRequestException(
                "Only failed media items can be retried.",
                SrsMediaErrorCodes.Unknown);
        }

        item.Status = MediaStatus.Queued;
        item.RetryCount += 1;
        item.ErrorCode = null;
        item.ErrorMessage = null;
        item.ProgressPercent = null;
        item.DownloadStartedAt = null;
        item.UpdatedAt = DateTimeOffset.UtcNow;

        await _media.UpdateAsync(item, cancellationToken);
        await _media.SaveChangesAsync(cancellationToken);

        await _jobPublisher.PublishDownloadJobAsync(
            new MediaDownloadJob
            {
                JobId = Guid.NewGuid(),
                MediaId = item.Id,
                UserId = item.UserId,
                Platform = item.Platform,
                OriginalUrl = item.OriginalUrl,
                Attempt = item.RetryCount,
                CreatedAt = DateTimeOffset.UtcNow,
            },
            cancellationToken);

        return item.ToResponse();
    }
}
