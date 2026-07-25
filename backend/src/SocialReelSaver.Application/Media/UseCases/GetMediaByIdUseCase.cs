using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Common.Exceptions;
using SocialReelSaver.Application.Media.DTOs;
using SocialReelSaver.Application.Media.Mappings;

namespace SocialReelSaver.Application.Media.UseCases;

public sealed class GetMediaByIdUseCase
{
    private readonly IMediaRepository _media;

    public GetMediaByIdUseCase(IMediaRepository media)
    {
        _media = media;
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

        return item.ToResponse();
    }
}
