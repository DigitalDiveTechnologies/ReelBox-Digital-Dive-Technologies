using SocialReelSaver.Domain.Entities;

namespace SocialReelSaver.Application.Abstractions.Playback;

public sealed record PlaybackMetadata
{
    public required Guid MediaId { get; init; }

    public required Guid UserId { get; init; }

    public required string Status { get; init; }

    public string? MediaStorageKey { get; init; }

    public string? ThumbnailStorageKey { get; init; }

    public string? MimeType { get; init; }

    public string? PlaybackUrl { get; init; }

    public string Delivery { get; init; } = "signed_url";

    public DateTimeOffset? ExpiresAt { get; init; }
}

public sealed record SignedUrlRequest
{
    public required Guid MediaId { get; init; }

    public required Guid UserId { get; init; }

    public required string StorageKey { get; init; }

    public string? MimeType { get; init; }

    public TimeSpan Lifetime { get; init; } = TimeSpan.FromMinutes(15);
}

public sealed record SignedUrlResult
{
    public bool Success { get; init; }

    public string? Url { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }

    public string Delivery { get; init; } = "signed_url";

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public static SignedUrlResult Ok(string url, DateTimeOffset expiresAt, string delivery) => new()
    {
        Success = true,
        Url = url,
        ExpiresAt = expiresAt,
        Delivery = delivery,
    };

    public static SignedUrlResult Failed(string code, string message) => new()
    {
        Success = false,
        ErrorCode = code,
        ErrorMessage = message,
    };
}

/// <summary>
/// Generates authorized playback URLs (SRS FR-014 / §16).
/// </summary>
public interface ISignedUrlProvider
{
    string ProviderName { get; }

    Task<SignedUrlResult> CreatePlaybackUrlAsync(
        SignedUrlRequest request,
        CancellationToken cancellationToken = default);

    bool TryValidatePlaybackToken(
        Guid mediaId,
        Guid userId,
        string storageKey,
        long expiresUnix,
        string signature);
}

public interface IPlaybackAuthorization
{
    void EnsureCanRequestPlayback(Guid userId, MediaItem item);
}

public interface IPlaybackUrlService
{
    Task<PlaybackMetadata> CreateAsync(
        MediaItem item,
        Guid requestingUserId,
        CancellationToken cancellationToken = default);
}
