using SocialReelSaver.Application.AdminAuth.DTOs;
using SocialReelSaver.Domain.Entities;

namespace SocialReelSaver.Application.AdminAuth.Mappings;

public static class AdminAuthMappings
{
    public static AdminProfileResponse ToProfileResponse(this AdminUser admin) =>
        new(
            admin.Id,
            admin.Email,
            admin.DisplayName,
            Roles: [admin.Role.ToString()],
            Permissions: Array.Empty<string>());

    public static AdminAuthResponse ToAuthResponse(
        this AdminUser admin,
        string accessToken,
        string refreshToken,
        int expiresInSeconds) =>
        new(
            admin.ToProfileResponse(),
            new AdminTokensResponse(accessToken, refreshToken, expiresInSeconds));
}
