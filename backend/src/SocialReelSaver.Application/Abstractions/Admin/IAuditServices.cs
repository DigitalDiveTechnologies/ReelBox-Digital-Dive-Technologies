using SocialReelSaver.Domain.Entities;

namespace SocialReelSaver.Application.Abstractions.Admin;

public interface IAuditLogWriter
{
    Task WriteAsync(
        Guid adminId,
        string adminEmail,
        string action,
        string entityType,
        string? entityId,
        object? oldValues,
        object? newValues,
        string? ipAddress,
        string? correlationId,
        CancellationToken cancellationToken = default);
}

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        string? search,
        Guid? adminId,
        string? action,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        string? sortBy = null,
        string? sortDir = null,
        CancellationToken cancellationToken = default);

    Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
