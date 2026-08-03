using Microsoft.EntityFrameworkCore;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Domain.Entities;
using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Infrastructure.Persistence.Repositories;

public sealed class AdminUserRepository : IAdminUserRepository
{
    private readonly AppDbContext _db;

    public AdminUserRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.AdminUsers.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<AdminUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _db.AdminUsers.FirstOrDefaultAsync(a => a.Email == normalized, cancellationToken);
    }

    public Task<AdminUser?> GetByRefreshTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default) =>
        _db.AdminUsers.FirstOrDefaultAsync(
            a => a.RefreshTokenHash == refreshTokenHash,
            cancellationToken);

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default) =>
        _db.AdminUsers.AnyAsync(cancellationToken);

    public async Task<(IReadOnlyList<AdminUser> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        string? search,
        AdminRole? role,
        bool? isActive,
        string? sortBy = null,
        string? sortDir = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.AdminUsers.AsNoTracking().AsQueryable();

        if (role is not null)
        {
            query = query.Where(a => a.Role == role.Value);
        }

        if (isActive is not null)
        {
            query = query.Where(a => a.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(a =>
                a.Email.ToLower().Contains(term) ||
                (a.DisplayName != null && a.DisplayName.ToLower().Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var asc = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
        query = (sortBy?.Trim().ToLowerInvariant()) switch
        {
            "email" => asc ? query.OrderBy(a => a.Email) : query.OrderByDescending(a => a.Email),
            "role" => asc ? query.OrderBy(a => a.Role) : query.OrderByDescending(a => a.Role),
            "status" or "isactive" => asc ? query.OrderBy(a => a.IsActive) : query.OrderByDescending(a => a.IsActive),
            "displayname" => asc ? query.OrderBy(a => a.DisplayName) : query.OrderByDescending(a => a.DisplayName),
            _ => asc ? query.OrderBy(a => a.CreatedAt) : query.OrderByDescending(a => a.CreatedAt),
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(AdminUser admin, CancellationToken cancellationToken = default)
    {
        await _db.AdminUsers.AddAsync(admin, cancellationToken);
    }

    public Task UpdateAsync(AdminUser admin, CancellationToken cancellationToken = default)
    {
        _db.AdminUsers.Update(admin);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
