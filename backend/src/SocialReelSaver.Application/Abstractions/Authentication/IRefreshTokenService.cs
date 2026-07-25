using SocialReelSaver.Domain.Entities;

namespace SocialReelSaver.Application.Abstractions.Authentication;

public interface IRefreshTokenService
{
    string HashToken(string refreshToken);

    Task StoreRefreshTokenAsync(
        User user,
        string refreshToken,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);

    bool ValidateRefreshToken(User user, string refreshToken);

    Task RevokeRefreshTokenAsync(
        User user,
        CancellationToken cancellationToken = default);
}
