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
        {
            query = query.Where(m => m.Status == status);
        }

        if (platform is not null)
        {
            query = query.Where(m => m.Platform == platform);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
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
