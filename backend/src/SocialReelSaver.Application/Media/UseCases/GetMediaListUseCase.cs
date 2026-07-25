using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Media.DTOs;
using SocialReelSaver.Application.Media.Mappings;
using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Application.Media.UseCases;

public sealed class GetMediaListUseCase
{
    private readonly IMediaRepository _media;

    public GetMediaListUseCase(IMediaRepository media)
    {
        _media = media;
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

        return items.ToListResponse(page, pageSize, total);
    }
}
