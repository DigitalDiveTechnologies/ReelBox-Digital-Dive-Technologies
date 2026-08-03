using SocialReelSaver.Application.Common;

namespace SocialReelSaver.Application.Admin.DTOs;

public sealed record AdminAccountListItem(
    Guid Id, string Email, string? DisplayName, string Role, bool IsActive,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record CreateAdminRequest(string Email, string Password, string? DisplayName, string Role);

public sealed record UpdateAdminRequest(string? DisplayName, string? Role, bool? IsActive);

public sealed record AdminAccountsListResponse(PagedResult<AdminAccountListItem> Result);
