using SocialReelSaver.Domain.Entities;

namespace SocialReelSaver.Application.Abstractions.Admin;

public interface IAppErrorLogWriter
{
    Task WriteAsync(
        string level,
        string message,
        string? detail,
        string? source,
        string? correlationId,
        string? path,
        int? statusCode,
        CancellationToken cancellationToken = default);
}

public interface IAppErrorLogRepository
{
    Task AddAsync(AppErrorLog log, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<AppErrorLog> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        string? search,
        string? level,
        string? correlationId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        string? sortBy = null,
        string? sortDir = null,
        CancellationToken cancellationToken = default);

    Task<AppErrorLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ISystemSettingRepository
{
    Task<IReadOnlyList<SystemSetting>> ListAllAsync(CancellationToken cancellationToken = default);

    Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    Task UpsertAsync(SystemSetting setting, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
