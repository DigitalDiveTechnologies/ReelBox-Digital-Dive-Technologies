using Microsoft.Extensions.Options;
using SocialReelSaver.Application.Abstractions.Playback;
using SocialReelSaver.Domain.Entities;
using SocialReelSaver.Domain.Enums;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Infrastructure.Playback;

public sealed class MediaThumbnailUrlService : IMediaThumbnailUrlService
{
    private readonly ISignedUrlProvider _signedUrls;
    private readonly ObjectStorageOptions _options;

    public MediaThumbnailUrlService(
        ISignedUrlProvider signedUrls,
        IOptions<ObjectStorageOptions> options)
    {
        _signedUrls = signedUrls;
        _options = options.Value;
    }

    public async Task<string?> CreateThumbnailUrlAsync(
        MediaItem item,
        CancellationToken cancellationToken = default)
    {
        if (item.Status != MediaStatus.Completed ||
            string.IsNullOrWhiteSpace(item.ThumbnailStorageKey))
        {
            return null;
        }

        var signed = await _signedUrls.CreatePlaybackUrlAsync(
            new SignedUrlRequest
            {
                MediaId = item.Id,
                UserId = item.UserId,
                StorageKey = item.ThumbnailStorageKey,
                MimeType = "image/jpeg",
                Lifetime = TimeSpan.FromMinutes(Math.Max(1, _options.PlaybackUrlExpirationMinutes)),
            },
            cancellationToken);

        return signed.Success ? signed.Url : null;
    }
}
