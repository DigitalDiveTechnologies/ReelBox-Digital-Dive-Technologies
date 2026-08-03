using SocialReelSaver.Application.Abstractions.Admin;
using SocialReelSaver.Application.Abstractions.Authentication;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Admin.DTOs;
using SocialReelSaver.Application.Common;
using SocialReelSaver.Application.Common.Exceptions;
using SocialReelSaver.Domain.Entities;
using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Application.Admin.UseCases;

public sealed class ListAdminAccountsUseCase(IAdminUserRepository admins)
{
    public async Task<PagedResult<AdminAccountListItem>> HandleAsync(int page, int pageSize, string? search, string? role, bool? isActive, string? sortBy = null, string? sortDir = null, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PagedResult<AdminAccountListItem>.Normalize(page, pageSize);
        var parsedRole = ParseRole(role, true);
        var result = await admins.ListAsync(page, pageSize, search, parsedRole, isActive, sortBy, sortDir, cancellationToken);
        return new(result.Items.Select(Map).ToList(), page, pageSize, result.TotalCount);
    }

    internal static AdminAccountListItem Map(AdminUser x) => new(x.Id, x.Email, x.DisplayName, x.Role.ToString(), x.IsActive, x.CreatedAt, x.UpdatedAt);
    internal static AdminRole? ParseRole(string? role, bool optional = false)
    {
        if (string.IsNullOrWhiteSpace(role) && optional) return null;
        if (Enum.TryParse<AdminRole>(role, true, out var value)) return value;
        throw new BadRequestException("Role is invalid.");
    }
}

public sealed class GetAdminAccountUseCase(IAdminUserRepository admins)
{
    public async Task<AdminAccountListItem> HandleAsync(Guid id, CancellationToken cancellationToken = default) =>
        ListAdminAccountsUseCase.Map(await admins.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Administrator was not found."));
}

public sealed class CreateAdminAccountUseCase(IAdminUserRepository admins, IPasswordHasher passwords, IAuditLogWriter audit)
{
    public async Task<AdminAccountListItem> HandleAsync(CreateAdminRequest request, Guid actorId, string actorEmail, string? ipAddress, string? correlationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password)) throw new BadRequestException("Email and password are required.");
        if (await admins.GetByEmailAsync(request.Email, cancellationToken) is not null) throw new ConflictException("Administrator email is already in use.");
        var admin = new AdminUser { Id = Guid.NewGuid(), Email = request.Email.Trim().ToLowerInvariant(), PasswordHash = passwords.HashPassword(request.Password), DisplayName = request.DisplayName?.Trim(), Role = ListAdminAccountsUseCase.ParseRole(request.Role)!.Value, IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
        await admins.AddAsync(admin, cancellationToken);
        await admins.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync(actorId, actorEmail, "admin.created", "AdminUser", admin.Id.ToString(), null, new { admin.Email, admin.DisplayName, role = admin.Role.ToString() }, ipAddress, correlationId, cancellationToken);
        return ListAdminAccountsUseCase.Map(admin);
    }
}

public sealed class UpdateAdminAccountUseCase(IAdminUserRepository admins, IAuditLogWriter audit)
{
    public async Task<AdminAccountListItem> HandleAsync(Guid id, UpdateAdminRequest request, Guid actorId, string actorEmail, string? ipAddress, string? correlationId, CancellationToken cancellationToken = default)
    {
        var admin = await admins.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Administrator was not found.");
        var old = new { admin.DisplayName, role = admin.Role.ToString(), admin.IsActive };
        if (request.DisplayName is not null) admin.DisplayName = request.DisplayName.Trim();
        if (request.Role is not null) admin.Role = ListAdminAccountsUseCase.ParseRole(request.Role)!.Value;
        if (request.IsActive is not null) { admin.IsActive = request.IsActive.Value; if (!admin.IsActive) { admin.RefreshTokenHash = null; admin.RefreshTokenExpiresAt = null; } }
        admin.UpdatedAt = DateTimeOffset.UtcNow;
        await admins.UpdateAsync(admin, cancellationToken);
        await admins.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync(actorId, actorEmail, "admin.updated", "AdminUser", id.ToString(), old, new { admin.DisplayName, role = admin.Role.ToString(), admin.IsActive }, ipAddress, correlationId, cancellationToken);
        return ListAdminAccountsUseCase.Map(admin);
    }
}

public sealed class AssignAdminRoleUseCase(UpdateAdminAccountUseCase update)
{
    public Task<AdminAccountListItem> HandleAsync(Guid id, string role, Guid actorId, string actorEmail, string? ipAddress, string? correlationId, CancellationToken cancellationToken = default) =>
        update.HandleAsync(id, new UpdateAdminRequest(null, role, null), actorId, actorEmail, ipAddress, correlationId, cancellationToken);
}
