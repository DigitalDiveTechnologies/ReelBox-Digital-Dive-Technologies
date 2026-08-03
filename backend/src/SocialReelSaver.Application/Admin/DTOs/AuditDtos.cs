using SocialReelSaver.Application.Common;

namespace SocialReelSaver.Application.Admin.DTOs;

public sealed record AuditLogListItem(
    Guid Id, Guid AdminId, string AdminEmail, string Action, string EntityType, string? EntityId,
    DateTimeOffset CreatedAt, string? IpAddress);

public sealed record AuditLogDetailResponse(
    Guid Id, Guid AdminId, string AdminEmail, string Action, string EntityType, string? EntityId,
    DateTimeOffset CreatedAt, string? IpAddress, string? OldValuesJson, string? NewValuesJson,
    string? CorrelationId);

public sealed record AuditLogsListResponse(PagedResult<AuditLogListItem> Result);
