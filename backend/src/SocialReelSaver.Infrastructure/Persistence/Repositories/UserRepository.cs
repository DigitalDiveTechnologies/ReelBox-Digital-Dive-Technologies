using Microsoft.EntityFrameworkCore;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Domain.Entities;
using SocialReelSaver.Infrastructure.Persistence;

namespace SocialReelSaver.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Users.Include(u => u.MediaItems).FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _db.Users.FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);
    }

    public Task<User?> GetByRefreshTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.RefreshTokenHash == refreshTokenHash, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _db.Users.AnyAsync(u => u.Email == normalized, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _db.Users.AddAsync(user, cancellationToken);
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        user.UpdatedAt = DateTimeOffset.UtcNow;
        _db.Users.Update(user);
        return Task.CompletedTask;
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        string? sortBy = null,
        string? sortDir = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Users.AsNoTracking().Include(u => u.MediaItems).AsQueryable();

        if (isActive is not null)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(u => u.Email.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var asc = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
        query = (sortBy?.Trim().ToLowerInvariant()) switch
        {
            "email" => asc ? query.OrderBy(u => u.Email) : query.OrderByDescending(u => u.Email),
            "status" or "isactive" => asc ? query.OrderBy(u => u.IsActive) : query.OrderByDescending(u => u.IsActive),
            "updatedat" => asc ? query.OrderBy(u => u.UpdatedAt) : query.OrderByDescending(u => u.UpdatedAt),
            _ => asc ? query.OrderBy(u => u.CreatedAt) : query.OrderByDescending(u => u.CreatedAt),
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
