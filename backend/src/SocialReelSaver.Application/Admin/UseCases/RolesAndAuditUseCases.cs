using SocialReelSaver.Application.Abstractions.Admin;
using SocialReelSaver.Application.Admin.DTOs;
using SocialReelSaver.Application.Admin.Roles;
using SocialReelSaver.Application.Common;
using SocialReelSaver.Application.Common.Exceptions;
using SocialReelSaver.Domain.Entities;

namespace SocialReelSaver.Application.Admin.UseCases;

public sealed class ListRolesUseCase
{
    public Task<RolesListResponse> HandleAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new RolesListResponse(AdminRoleCatalog.Definitions));
}

public sealed class ListAuditLogsUseCase(IAuditLogRepository logs)
{
    public async Task<PagedResult<AuditLogListItem>> HandleAsync(int page, int pageSize, string? search, Guid? adminId, string? action, DateTimeOffset? fromUtc, DateTimeOffset? toUtc, string? sortBy = null, string? sortDir = null, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PagedResult<AuditLogListItem>.Normalize(page, pageSize);
        var result = await logs.ListAsync(page, pageSize, search, adminId, action, fromUtc, toUtc, sortBy, sortDir, cancellationToken);
        return new(result.Items.Select(Map).ToList(), page, pageSize, result.TotalCount);
    }

    internal static AuditLogListItem Map(AuditLog x) => new(x.Id, x.AdminId, x.AdminEmail, x.Action, x.EntityType, x.EntityId, x.CreatedAt, x.IpAddress);
}

public sealed class GetAuditLogUseCase(IAuditLogRepository logs)
{
    public async Task<AuditLogDetailResponse> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var log = await logs.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Audit log entry was not found.");
        return new(log.Id, log.AdminId, log.AdminEmail, log.Action, log.EntityType, log.EntityId, log.CreatedAt, log.IpAddress, log.OldValuesJson, log.NewValuesJson, log.CorrelationId);
    }
}
