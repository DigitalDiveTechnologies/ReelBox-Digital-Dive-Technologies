using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using SocialReelSaver.Application.Abstractions.Playback;
using SocialReelSaver.Application.Abstractions.Storage;
using SocialReelSaver.Application.Common.Exceptions;
using SocialReelSaver.Application.Media.Errors;
using SocialReelSaver.Domain.Entities;
using SocialReelSaver.Domain.Enums;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Infrastructure.Playback;

public sealed class PlaybackAuthorization : IPlaybackAuthorization
{
    public void EnsureCanRequestPlayback(Guid userId, MediaItem item)
    {
        if (item.UserId != userId)
        {
            throw new NotFoundException("Media item not found.");
        }

        if (item.Status != MediaStatus.Completed)
        {
            throw new BadRequestException(
                "Playback is only available for completed media.",
                SrsMediaErrorCodes.Unknown);
        }

        if (string.IsNullOrWhiteSpace(item.MediaStorageKey))
        {
            throw new BadRequestException(
                "Completed media is missing a storage key.",
                SrsMediaErrorCodes.StorageFailure);
        }
    }
}

/// <summary>
/// Local application playback URLs (HMAC-signed API content route).
/// </summary>
public sealed class LocalSignedUrlProvider : ISignedUrlProvider
{
    private readonly ObjectStorageOptions _storageOptions;
    private readonly JwtOptions _jwtOptions;

    public LocalSignedUrlProvider(
        IOptions<ObjectStorageOptions> storageOptions,
        IOptions<JwtOptions> jwtOptions)
    {
        _storageOptions = storageOptions.Value;
        _jwtOptions = jwtOptions.Value;
    }

    public string ProviderName => "Local";

    public Task<SignedUrlResult> CreatePlaybackUrlAsync(
        SignedUrlRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var lifetimeMinutes = _storageOptions.PlaybackUrlExpirationMinutes > 0
            ? _storageOptions.PlaybackUrlExpirationMinutes
            : 15;
        var lifetime = request.Lifetime <= TimeSpan.Zero
            ? TimeSpan.FromMinutes(lifetimeMinutes)
            : request.Lifetime;

        var expiresAt = DateTimeOffset.UtcNow.Add(lifetime);
        var exp = expiresAt.ToUnixTimeSeconds();
        var signature = ComputeSignature(request.MediaId, request.UserId, request.StorageKey, exp);

        var baseUrl = string.IsNullOrWhiteSpace(_storageOptions.PublicApiBaseUrl)
            ? string.Empty
            : _storageOptions.PublicApiBaseUrl.TrimEnd('/');

        var path =
            $"/api/v1/media/{request.MediaId:D}/content?uid={request.UserId:D}&key={Uri.EscapeDataString(request.StorageKey)}&exp={exp}&sig={Uri.EscapeDataString(signature)}";

        var url = string.IsNullOrWhiteSpace(baseUrl) ? path : baseUrl + path;
        return Task.FromResult(SignedUrlResult.Ok(url, expiresAt, delivery: "application_signed_url"));
    }

    public bool TryValidatePlaybackToken(
        Guid mediaId,
        Guid userId,
        string storageKey,
        long expiresUnix,
        string signature)
    {
        if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(storageKey))
        {
            return false;
        }

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresUnix);
        if (expiresAt < DateTimeOffset.UtcNow.AddSeconds(-30))
        {
            return false;
        }

        var expected = ComputeSignature(mediaId, userId, storageKey, expiresUnix);
        var expectedBytes = Convert.FromHexString(expected);
        byte[] actualBytes;
        try
        {
            actualBytes = Convert.FromHexString(signature);
        }
        catch (FormatException)
        {
            return false;
        }

        return expectedBytes.Length == actualBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    private string ComputeSignature(Guid mediaId, Guid userId, string storageKey, long exp)
    {
        var keyMaterial = string.IsNullOrWhiteSpace(_storageOptions.PlaybackSigningKey)
            ? _jwtOptions.SigningKey
            : _storageOptions.PlaybackSigningKey;

        if (string.IsNullOrWhiteSpace(keyMaterial))
        {
            throw new InvalidOperationException("Playback signing key is not configured.");
        }

        var payload = $"{mediaId:N}|{userId:N}|{storageKey}|{exp}";
        var hash = System.Security.Cryptography.HMACSHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(keyMaterial),
            System.Text.Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

/// <summary>
/// Placeholder signed-URL provider for S3/R2 configurations.
/// </summary>
public sealed class CloudSignedUrlProvider : ISignedUrlProvider
{
    private readonly ObjectStorageOptions _options;

    public CloudSignedUrlProvider(IOptions<ObjectStorageOptions> options)
    {
        _options = options.Value;
    }

    public string ProviderName => _options.Provider;

    public Task<SignedUrlResult> CreatePlaybackUrlAsync(
        SignedUrlRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // TODO: Generate cloud provider signed GET URL (SRS FR-014 / §16).
        return Task.FromResult(SignedUrlResult.Failed(
            SrsMediaErrorCodes.StorageFailure,
            $"Signed URL generation for '{_options.Provider}' is not implemented yet."));
    }

    public bool TryValidatePlaybackToken(
        Guid mediaId,
        Guid userId,
        string storageKey,
        long expiresUnix,
        string signature) => false;
}

public sealed class PlaybackUrlService : IPlaybackUrlService
{
    private readonly IPlaybackAuthorization _authorization;
    private readonly ISignedUrlProvider _signedUrls;
    private readonly IObjectStorageService _storage;
    private readonly ObjectStorageOptions _options;

    public PlaybackUrlService(
        IPlaybackAuthorization authorization,
        ISignedUrlProvider signedUrls,
        IObjectStorageService storage,
        IOptions<ObjectStorageOptions> options)
    {
        _authorization = authorization;
        _signedUrls = signedUrls;
        _storage = storage;
        _options = options.Value;
    }

    public async Task<PlaybackMetadata> CreateAsync(
        MediaItem item,
        Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        _authorization.EnsureCanRequestPlayback(requestingUserId, item);

        if (_storage.ProviderName.Equals("Local", StringComparison.OrdinalIgnoreCase))
        {
            var exists = await _storage.ExistsAsync(item.MediaStorageKey!, cancellationToken);
            if (!exists)
            {
                throw new BadRequestException(
                    "Media object is missing from storage.",
                    SrsMediaErrorCodes.StorageFailure);
            }
        }

        var signed = await _signedUrls.CreatePlaybackUrlAsync(
            new SignedUrlRequest
            {
                MediaId = item.Id,
                UserId = requestingUserId,
                StorageKey = item.MediaStorageKey!,
                MimeType = item.MimeType,
                Lifetime = TimeSpan.FromMinutes(Math.Max(1, _options.PlaybackUrlExpirationMinutes)),
            },
            cancellationToken);

        if (!signed.Success)
        {
            throw new BadRequestException(
                signed.ErrorMessage ?? "Unable to create playback URL.",
                signed.ErrorCode ?? "STORAGE_FAILURE");
        }

        return new PlaybackMetadata
        {
            MediaId = item.Id,
            UserId = requestingUserId,
            Status = item.Status.ToString().ToLowerInvariant(),
            MediaStorageKey = item.MediaStorageKey,
            ThumbnailStorageKey = item.ThumbnailStorageKey,
            MimeType = item.MimeType,
            PlaybackUrl = signed.Url,
            Delivery = signed.Delivery,
            ExpiresAt = signed.ExpiresAt,
        };
    }
}
