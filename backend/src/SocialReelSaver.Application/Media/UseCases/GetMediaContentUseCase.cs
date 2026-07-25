using SocialReelSaver.Application.Abstractions.Playback;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Abstractions.Storage;
using SocialReelSaver.Application.Common.Exceptions;
using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Application.Media.UseCases;

public sealed class GetMediaContentUseCase
{
    private readonly IMediaRepository _media;
    private readonly IObjectStorageService _storage;
    private readonly ISignedUrlProvider _signedUrls;

    public GetMediaContentUseCase(
        IMediaRepository media,
        IObjectStorageService storage,
        ISignedUrlProvider signedUrls)
    {
        _media = media;
        _storage = storage;
        _signedUrls = signedUrls;
    }

    public async Task<MediaContentResult> HandleAsync(
        Guid mediaId,
        Guid userId,
        string storageKey,
        long expiresUnix,
        string signature,
        CancellationToken cancellationToken = default)
    {
        if (!_signedUrls.TryValidatePlaybackToken(mediaId, userId, storageKey, expiresUnix, signature))
        {
            throw new UnauthorizedAppException("Playback URL is invalid or expired.");
        }

        var item = await _media.GetByIdForUserAsync(mediaId, userId, cancellationToken);
        if (item is null || item.Status != MediaStatus.Completed)
        {
            throw new NotFoundException("Media item not found.");
        }

        if (!string.Equals(item.MediaStorageKey, storageKey, StringComparison.Ordinal))
        {
            throw new UnauthorizedAppException("Playback URL does not match media storage key.");
        }

        var storageObject = await _storage.OpenObjectAsync(storageKey, cancellationToken);
        if (storageObject is null)
        {
            throw new NotFoundException("Media object was not found in storage.");
        }

        return new MediaContentResult(
            storageObject.Content,
            storageObject.Metadata.ContentType ?? item.MimeType ?? "application/octet-stream",
            Path.GetFileName(storageKey));
    }
}

public sealed record MediaContentResult(Stream Content, string ContentType, string FileName);
