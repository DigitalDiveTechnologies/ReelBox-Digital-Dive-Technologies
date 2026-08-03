using SocialReelSaver.Domain.Entities;
using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Application.Abstractions.Persistence;

public interface IAdminUserRepository
{
    Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AdminUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<AdminUser?> GetByRefreshTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<AdminUser> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        string? search,
        AdminRole? role,
        bool? isActive,
        string? sortBy = null,
        string? sortDir = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(AdminUser admin, CancellationToken cancellationToken = default);

    Task UpdateAsync(AdminUser admin, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
