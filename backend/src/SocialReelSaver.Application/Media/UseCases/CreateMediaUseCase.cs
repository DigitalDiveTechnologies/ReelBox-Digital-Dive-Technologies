using SocialReelSaver.Application.Abstractions.Media;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Abstractions.Queue;
using SocialReelSaver.Application.Media.DTOs;
using SocialReelSaver.Application.Media.Jobs;
using SocialReelSaver.Application.Media.Mappings;
using SocialReelSaver.Domain.Entities;
using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Application.Media.UseCases;

public sealed class CreateMediaUseCase
{
    private readonly IMediaRepository _media;
    private readonly IMediaUrlAnalyzer _urlAnalyzer;
    private readonly IMediaJobPublisher _jobPublisher;

    public CreateMediaUseCase(
        IMediaRepository media,
        IMediaUrlAnalyzer urlAnalyzer,
        IMediaJobPublisher jobPublisher)
    {
        _media = media;
        _urlAnalyzer = urlAnalyzer;
        _jobPublisher = jobPublisher;
    }

    public async Task<(MediaResponse Response, bool Created)> HandleAsync(
        Guid userId,
        CreateMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        var analysis = _urlAnalyzer.Analyze(request.Url);

        var existing = await _media.GetByNormalizedUrlAsync(
            userId,
            analysis.NormalizedUrl,
            cancellationToken);

        if (existing is not null)
        {
            return (existing.ToResponse(request.Source), Created: false);
        }

        var now = DateTimeOffset.UtcNow;
        var item = new MediaItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OriginalUrl = analysis.OriginalUrl,
            NormalizedUrl = analysis.NormalizedUrl,
            Platform = analysis.Platform,
            Status = MediaStatus.Preparing,
            CreatedAt = now,
            UpdatedAt = now,
            RetryCount = 0,
        };

        await _media.AddAsync(item, cancellationToken);
        await _media.SaveChangesAsync(cancellationToken);

        item.Status = MediaStatus.Queued;
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
                Attempt = 0,
                CreatedAt = DateTimeOffset.UtcNow,
            },
            cancellationToken);

        return (item.ToResponse(request.Source), Created: true);
    }
}
