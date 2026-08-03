using Microsoft.EntityFrameworkCore;
using SocialReelSaver.Application.Abstractions.Admin;
using SocialReelSaver.Domain.Entities;
using SocialReelSaver.Infrastructure.Persistence;

namespace SocialReelSaver.Infrastructure.Persistence.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _db;

    public AuditLogRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(AuditLog log, CancellationToken cancellationToken = default)
    {
        await _db.AuditLogs.AddAsync(log, cancellationToken);
    }

    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        string? search,
        Guid? adminId,
        string? action,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        string? sortBy = null,
        string? sortDir = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (adminId is not null)
        {
            query = query.Where(x => x.AdminId == adminId.Value);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            var a = action.Trim();
            query = query.Where(x => x.Action == a);
        }

        if (fromUtc is not null)
        {
            query = query.Where(x => x.CreatedAt >= fromUtc.Value);
        }

        if (toUtc is not null)
        {
            query = query.Where(x => x.CreatedAt <= toUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.AdminEmail.ToLower().Contains(term) ||
                x.Action.ToLower().Contains(term) ||
                x.EntityType.ToLower().Contains(term) ||
                (x.EntityId != null && x.EntityId.ToLower().Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var asc = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
        query = (sortBy?.Trim().ToLowerInvariant()) switch
        {
            "adminemail" => asc ? query.OrderBy(x => x.AdminEmail) : query.OrderByDescending(x => x.AdminEmail),
            "action" => asc ? query.OrderBy(x => x.Action) : query.OrderByDescending(x => x.Action),
            "entitytype" => asc ? query.OrderBy(x => x.EntityType) : query.OrderByDescending(x => x.EntityType),
            _ => asc ? query.OrderBy(x => x.CreatedAt) : query.OrderByDescending(x => x.CreatedAt),
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.AuditLogs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
