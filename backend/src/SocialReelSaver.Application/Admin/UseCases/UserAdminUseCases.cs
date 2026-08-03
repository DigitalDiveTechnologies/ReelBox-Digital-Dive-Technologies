using SocialReelSaver.Application.Abstractions.Admin;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Admin.DTOs;
using SocialReelSaver.Application.Common;
using SocialReelSaver.Application.Common.Exceptions;

namespace SocialReelSaver.Application.Admin.UseCases;

public sealed class ListUsersAdminUseCase(IUserRepository users)
{
    public async Task<PagedResult<AdminUserListItem>> HandleAsync(int page, int pageSize, string? search, bool? isActive, string? sortBy = null, string? sortDir = null, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PagedResult<AdminUserListItem>.Normalize(page, pageSize);
        var result = await users.ListAsync(page, pageSize, search, isActive, sortBy, sortDir, cancellationToken);
        return new(result.Items.Select(x => new AdminUserListItem(x.Id, x.Email, x.IsActive, x.CreatedAt, x.UpdatedAt, x.MediaItems.Count)).ToList(), page, pageSize, result.TotalCount);
    }
}

public sealed class GetUserAdminUseCase(IUserRepository users)
{
    public async Task<AdminUserDetailResponse> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("User was not found.");
        return new(user.Id, user.Email, user.IsActive, user.CreatedAt, user.UpdatedAt, user.MediaItems.Count, user.RefreshTokenHash is not null);
    }
}

public sealed class UpdateUserStatusUseCase(IUserRepository users, IAuditLogWriter audit)
{
    public async Task HandleAsync(Guid id, bool isActive, Guid adminId, string adminEmail, string? ipAddress, string? correlationId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("User was not found.");
        var old = user.IsActive;
        user.IsActive = isActive;
        if (!isActive) { user.RefreshTokenHash = null; user.RefreshTokenExpiresAt = null; }
        await users.UpdateAsync(user, cancellationToken);
        await users.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync(adminId, adminEmail, "user.status.updated", "User", id.ToString(), new { isActive = old }, new { isActive }, ipAddress, correlationId, cancellationToken);
    }
}

public sealed class RevokeUserSessionsUseCase(IUserRepository users, IAuditLogWriter audit)
{
    public async Task HandleAsync(Guid id, Guid adminId, string adminEmail, string? ipAddress, string? correlationId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("User was not found.");
        user.RefreshTokenHash = null;
        user.RefreshTokenExpiresAt = null;
        await users.UpdateAsync(user, cancellationToken);
        await users.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync(adminId, adminEmail, "user.sessions.revoked", "User", id.ToString(), null, null, ipAddress, correlationId, cancellationToken);
    }
}
