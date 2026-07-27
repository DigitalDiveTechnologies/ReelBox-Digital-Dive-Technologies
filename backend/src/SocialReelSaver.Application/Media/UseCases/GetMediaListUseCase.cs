using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Abstractions.Playback;
using SocialReelSaver.Application.Media.DTOs;
using SocialReelSaver.Application.Media.Mappings;
using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Application.Media.UseCases;

public sealed class GetMediaListUseCase
{
    private readonly IMediaRepository _media;
    private readonly IMediaThumbnailUrlService _thumbnailUrls;

    public GetMediaListUseCase(
        IMediaRepository media,
        IMediaThumbnailUrlService thumbnailUrls)
    {
        _media = media;
        _thumbnailUrls = thumbnailUrls;
    }

    public async Task<MediaListResponse> HandleAsync(
        Guid userId,
        int page,
        int pageSize,
        MediaStatus? status,
        MediaPlatform? platform,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

        var (items, total) = await _media.ListForUserAsync(
            userId,
            page,
            pageSize,
            status,
            platform,
            cancellationToken);

        var thumbs = new Dictionary<Guid, string?>();
        foreach (var item in items)
        {
            thumbs[item.Id] = await _thumbnailUrls.CreateThumbnailUrlAsync(item, cancellationToken);
        }

        return items.ToListResponse(page, pageSize, total, thumbs);
    }
}
