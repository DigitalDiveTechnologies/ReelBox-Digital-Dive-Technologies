using System.Security.Cryptography;
using System.Text;
using SocialReelSaver.Application.Abstractions.Authentication;
using SocialReelSaver.Domain.Entities;

namespace SocialReelSaver.Infrastructure.Authentication;

public sealed class RefreshTokenService : IRefreshTokenService
{
    public string HashToken(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes);
    }

    public Task StoreRefreshTokenAsync(
        User user,
        string refreshToken,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        user.RefreshTokenHash = HashToken(refreshToken);
        user.RefreshTokenExpiresAt = expiresAt;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }

    public bool ValidateRefreshToken(User user, string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(user.RefreshTokenHash) || user.RefreshTokenExpiresAt is null)
        {
            return false;
        }

        if (user.RefreshTokenExpiresAt.Value <= DateTimeOffset.UtcNow)
        {
            return false;
        }

        var incomingHash = HashToken(refreshToken);
        var stored = Convert.FromHexString(user.RefreshTokenHash);
        var incoming = Convert.FromHexString(incomingHash);
        return CryptographicOperations.FixedTimeEquals(stored, incoming);
    }

    public Task RevokeRefreshTokenAsync(User user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        user.RefreshTokenHash = null;
        user.RefreshTokenExpiresAt = null;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }
}
