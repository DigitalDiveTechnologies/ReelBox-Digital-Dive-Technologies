using SocialReelSaver.Application.Common;

namespace SocialReelSaver.Application.Admin.DTOs;

public sealed record AdminUserListItem(
    Guid Id, string Email, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, int MediaCount);

public sealed record AdminUserDetailResponse(
    Guid Id, string Email, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    int MediaCount, bool HasActiveSession);

public sealed record UpdateUserStatusRequest(bool IsActive);

public sealed record UsersListResponse(PagedResult<AdminUserListItem> Result);
