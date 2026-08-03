using Microsoft.EntityFrameworkCore;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Domain.Entities;
using SocialReelSaver.Domain.Enums;
using SocialReelSaver.Infrastructure.Persistence;

namespace SocialReelSaver.Infrastructure.Persistence.Repositories;

public sealed class MediaRepository : IMediaRepository
{
    private readonly AppDbContext _db;

    public MediaRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<MediaItem?> GetByIdAsync(
        Guid mediaId,
        CancellationToken cancellationToken = default) =>
        _db.MediaItems.FirstOrDefaultAsync(m => m.Id == mediaId, cancellationToken);

    public Task<MediaItem?> GetByIdWithUserAsync(
        Guid mediaId,
        CancellationToken cancellationToken = default) =>
        _db.MediaItems.Include(m => m.User).FirstOrDefaultAsync(m => m.Id == mediaId, cancellationToken);

    public Task<MediaItem?> GetByIdForUserAsync(
        Guid mediaId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _db.MediaItems.FirstOrDefaultAsync(
            m => m.Id == mediaId && m.UserId == userId,
            cancellationToken);

    public Task<MediaItem?> GetByNormalizedUrlAsync(
        Guid userId,
        string normalizedUrl,
        CancellationToken cancellationToken = default) =>
        _db.MediaItems.FirstOrDefaultAsync(
            m => m.UserId == userId && m.NormalizedUrl == normalizedUrl,
            cancellationToken);

    public async Task<(IReadOnlyList<MediaItem> Items, int TotalCount)> ListForUserAsync(
        Guid userId,
        int page,
        int pageSize,
        MediaStatus? status,
        MediaPlatform? platform,
        CancellationToken cancellationToken = default)
    {
        var query = _db.MediaItems.AsNoTracking().Where(m => m.UserId == userId);

        if (status is not null)
            query = query.Where(m => m.Status == status);

        if (platform is not null)
            query = query.Where(m => m.Platform == platform);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(IReadOnlyList<MediaItem> Items, int TotalCount)> ListAdminAsync(
        int page,
        int pageSize,
        string? search,
        MediaStatus? status,
        MediaPlatform? platform,
        Guid? userId,
        IReadOnlyList<MediaStatus>? statusIn,
        string? sortBy = null,
        string? sortDir = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.MediaItems.AsNoTracking().Include(m => m.User).AsQueryable();

        if (status is not null)
            query = query.Where(m => m.Status == status);
        if (statusIn is { Count: > 0 })
            query = query.Where(m => statusIn.Contains(m.Status));
        if (platform is not null)
            query = query.Where(m => m.Platform == platform);
        if (userId is not null)
            query = query.Where(m => m.UserId == userId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(m =>
                m.OriginalUrl.ToLower().Contains(term)
                || (m.Title != null && m.Title.ToLower().Contains(term))
                || (m.User != null && m.User.Email.ToLower().Contains(term))
                || m.Id.ToString().ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var asc = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
        query = (sortBy?.Trim().ToLowerInvariant()) switch
        {
            "status" => asc ? query.OrderBy(m => m.Status) : query.OrderByDescending(m => m.Status),
            "platform" => asc ? query.OrderBy(m => m.Platform) : query.OrderByDescending(m => m.Platform),
            "updatedat" => asc ? query.OrderBy(m => m.UpdatedAt) : query.OrderByDescending(m => m.UpdatedAt),
            "filesizebytes" => asc ? query.OrderBy(m => m.FileSizeBytes) : query.OrderByDescending(m => m.FileSizeBytes),
            _ => asc ? query.OrderBy(m => m.CreatedAt) : query.OrderByDescending(m => m.CreatedAt),
        };

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<IReadOnlyDictionary<MediaStatus, int>> StatusCountsAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.MediaItems.AsNoTracking()
            .GroupBy(m => m.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(x => x.Status, x => x.Count);
    }

    public async Task<long> SumFileSizeBytesAsync(CancellationToken cancellationToken = default) =>
        await _db.MediaItems.AsNoTracking().SumAsync(m => m.FileSizeBytes ?? 0L, cancellationToken);

    public async Task<IReadOnlyList<(string? MediaStorageKey, string? ThumbnailStorageKey)>> ListStorageKeysAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.MediaItems.AsNoTracking()
            .Where(m => m.MediaStorageKey != null || m.ThumbnailStorageKey != null)
            .Select(m => new { m.MediaStorageKey, m.ThumbnailStorageKey })
            .ToListAsync(cancellationToken);
        return rows.Select(x => (x.MediaStorageKey, x.ThumbnailStorageKey)).ToList();
    }

    public async Task AddAsync(MediaItem item, CancellationToken cancellationToken = default) =>
        await _db.MediaItems.AddAsync(item, cancellationToken);

    public Task UpdateAsync(MediaItem item, CancellationToken cancellationToken = default)
    {
        _db.MediaItems.Update(item);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(MediaItem item, CancellationToken cancellationToken = default)
    {
        _db.MediaItems.Remove(item);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
