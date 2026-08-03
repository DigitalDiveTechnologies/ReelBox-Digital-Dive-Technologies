using SocialReelSaver.Domain.Entities;
using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Application.Abstractions.Persistence;

public interface IMediaRepository
{
    Task<MediaItem?> GetByIdAsync(
        Guid mediaId,
        CancellationToken cancellationToken = default);

    Task<MediaItem?> GetByIdWithUserAsync(
        Guid mediaId,
        CancellationToken cancellationToken = default);

    Task<MediaItem?> GetByIdForUserAsync(
        Guid mediaId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<MediaItem?> GetByNormalizedUrlAsync(
        Guid userId,
        string normalizedUrl,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<MediaItem> Items, int TotalCount)> ListForUserAsync(
        Guid userId,
        int page,
        int pageSize,
        MediaStatus? status,
        MediaPlatform? platform,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<MediaItem> Items, int TotalCount)> ListAdminAsync(
        int page,
        int pageSize,
        string? search,
        MediaStatus? status,
        MediaPlatform? platform,
        Guid? userId,
        IReadOnlyList<MediaStatus>? statusIn,
        string? sortBy = null,
        string? sortDir = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<MediaStatus, int>> StatusCountsAsync(
        CancellationToken cancellationToken = default);

    Task<long> SumFileSizeBytesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(string? MediaStorageKey, string? ThumbnailStorageKey)>> ListStorageKeysAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(MediaItem item, CancellationToken cancellationToken = default);

    Task UpdateAsync(MediaItem item, CancellationToken cancellationToken = default);

    Task DeleteAsync(MediaItem item, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
