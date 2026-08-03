using SocialReelSaver.Domain.Entities;
using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Application.Abstractions.Authentication;

public interface IAdminJwtTokenService
{
    Task<string> CreateAccessTokenAsync(
        Guid adminId,
        string email,
        AdminRole role,
        CancellationToken cancellationToken = default);

    Task<string> CreateRefreshTokenAsync(CancellationToken cancellationToken = default);

    DateTimeOffset GetAccessTokenExpiry();

    DateTimeOffset GetRefreshTokenExpiry();

    int GetAccessTokenExpiresInSeconds();
}

public interface IAdminRefreshTokenService
{
    string HashToken(string refreshToken);

    Task StoreRefreshTokenAsync(
        AdminUser admin,
        string refreshToken,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);

    bool ValidateRefreshToken(AdminUser admin, string refreshToken);

    Task RevokeRefreshTokenAsync(AdminUser admin, CancellationToken cancellationToken = default);
}

public interface IAdminAuthenticationService
{
    Task<AdminUser?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);
}
