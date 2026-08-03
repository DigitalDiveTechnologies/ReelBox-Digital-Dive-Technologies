using Microsoft.EntityFrameworkCore;
using SocialReelSaver.Application.Abstractions.Admin;
using SocialReelSaver.Domain.Entities;
using SocialReelSaver.Infrastructure.Persistence;

namespace SocialReelSaver.Infrastructure.Admin;

public sealed class AppErrorLogWriter(AppDbContext db) : IAppErrorLogWriter
{
    public async Task WriteAsync(
        string level, string message, string? detail, string? source,
        string? correlationId, string? path, int? statusCode,
        CancellationToken cancellationToken = default)
    {
        db.AppErrorLogs.Add(new AppErrorLog
        {
            Id = Guid.NewGuid(),
            Level = level,
            Message = Truncate(message, 2000) ?? string.Empty,
            Detail = Truncate(detail, 16000),
            Source = Truncate(source, 256),
            CorrelationId = Truncate(correlationId, 64),
            Path = Truncate(path, 512),
            StatusCode = statusCode,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string? Truncate(string? value, int max) =>
        value is null ? null : value.Length <= max ? value : value[..max];
}

public sealed class AppErrorLogRepository(AppDbContext db) : IAppErrorLogRepository
{
    public async Task AddAsync(AppErrorLog log, CancellationToken cancellationToken = default) =>
        await db.AppErrorLogs.AddAsync(log, cancellationToken);

    public async Task<(IReadOnlyList<AppErrorLog> Items, int TotalCount)> ListAsync(
        int page, int pageSize, string? search, string? level, string? correlationId,
        DateTimeOffset? fromUtc, DateTimeOffset? toUtc, string? sortBy = null, string? sortDir = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.AppErrorLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(level))
            query = query.Where(x => x.Level == level);
        if (!string.IsNullOrWhiteSpace(correlationId))
            query = query.Where(x => x.CorrelationId == correlationId);
        if (fromUtc is not null)
            query = query.Where(x => x.CreatedAt >= fromUtc);
        if (toUtc is not null)
            query = query.Where(x => x.CreatedAt <= toUtc);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(x => x.Message.ToLower().Contains(term) || (x.Source != null && x.Source.ToLower().Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var asc = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
        query = (sortBy?.Trim().ToLowerInvariant()) switch
        {
            "level" => asc ? query.OrderBy(x => x.Level) : query.OrderByDescending(x => x.Level),
            "statuscode" => asc ? query.OrderBy(x => x.StatusCode) : query.OrderByDescending(x => x.StatusCode),
            _ => asc ? query.OrderBy(x => x.CreatedAt) : query.OrderByDescending(x => x.CreatedAt),
        };

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<AppErrorLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.AppErrorLogs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}

public sealed class SystemSettingRepository(AppDbContext db) : ISystemSettingRepository
{
    public async Task<IReadOnlyList<SystemSetting>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await db.SystemSettings.AsNoTracking().ToListAsync(cancellationToken);

    public Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default) =>
        db.SystemSettings.FirstOrDefaultAsync(x => x.Key == key, cancellationToken);

    public async Task UpsertAsync(SystemSetting setting, CancellationToken cancellationToken = default)
    {
        var existing = await db.SystemSettings.FirstOrDefaultAsync(x => x.Key == setting.Key, cancellationToken);
        if (existing is null)
        {
            await db.SystemSettings.AddAsync(setting, cancellationToken);
            return;
        }

        existing.Value = setting.Value;
        existing.Category = setting.Category;
        existing.UpdatedAt = setting.UpdatedAt;
        existing.UpdatedByAdminId = setting.UpdatedByAdminId;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
