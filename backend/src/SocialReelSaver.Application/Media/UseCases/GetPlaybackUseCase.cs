using SocialReelSaver.Application.Abstractions.Playback;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Common.Exceptions;
using SocialReelSaver.Application.Media.DTOs;

namespace SocialReelSaver.Application.Media.UseCases;

public sealed class GetPlaybackUseCase
{
    private readonly IMediaRepository _media;
    private readonly IPlaybackUrlService _playbackUrls;

    public GetPlaybackUseCase(
        IMediaRepository media,
        IPlaybackUrlService playbackUrls)
    {
        _media = media;
        _playbackUrls = playbackUrls;
    }

    public async Task<PlaybackResponse> HandleAsync(
        Guid userId,
        Guid mediaId,
        CancellationToken cancellationToken = default)
    {
        var item = await _media.GetByIdForUserAsync(mediaId, userId, cancellationToken);
        if (item is null)
        {
            throw new NotFoundException("Media item not found.");
        }

        var metadata = await _playbackUrls.CreateAsync(item, userId, cancellationToken);

        return new PlaybackResponse(
            metadata.MediaId,
            metadata.Status,
            metadata.MediaStorageKey,
            metadata.ThumbnailStorageKey,
            metadata.MimeType,
            metadata.PlaybackUrl,
            metadata.Delivery,
            metadata.ExpiresAt);
    }
}
