using SocialReelSaver.Application.Auth.DTOs;
using SocialReelSaver.Domain.Entities;

namespace SocialReelSaver.Application.Auth.Mappings;

public static class AuthMappings
{
    public static UserResponse ToResponse(this User user) =>
        new(user.Id, user.Email, user.CreatedAt);

    public static AuthResponse ToAuthResponse(
        this User user,
        string accessToken,
        string refreshToken,
        DateTimeOffset accessTokenExpiresAt,
        DateTimeOffset refreshTokenExpiresAt) =>
        new(
            user.ToResponse(),
            new AuthTokensResponse(
                accessToken,
                refreshToken,
                accessTokenExpiresAt,
                refreshTokenExpiresAt));
}
