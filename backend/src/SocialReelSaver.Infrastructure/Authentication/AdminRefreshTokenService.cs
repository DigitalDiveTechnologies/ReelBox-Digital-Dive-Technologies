using System.Security.Cryptography;
using System.Text;
using SocialReelSaver.Application.Abstractions.Authentication;
using SocialReelSaver.Domain.Entities;

namespace SocialReelSaver.Infrastructure.Authentication;

/// <summary>
/// Admin refresh-token hashing — same algorithm as mobile, isolated to <see cref="AdminUser"/>.
/// </summary>
public sealed class AdminRefreshTokenService : IAdminRefreshTokenService
{
    public string HashToken(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes);
    }

    public Task StoreRefreshTokenAsync(
        AdminUser admin,
        string refreshToken,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        admin.RefreshTokenHash = HashToken(refreshToken);
        admin.RefreshTokenExpiresAt = expiresAt;
        admin.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }

    public bool ValidateRefreshToken(AdminUser admin, string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(admin.RefreshTokenHash) || admin.RefreshTokenExpiresAt is null)
        {
            return false;
        }

        if (admin.RefreshTokenExpiresAt.Value <= DateTimeOffset.UtcNow)
        {
            return false;
        }

        var incomingHash = HashToken(refreshToken);
        var stored = Convert.FromHexString(admin.RefreshTokenHash);
        var incoming = Convert.FromHexString(incomingHash);
        return CryptographicOperations.FixedTimeEquals(stored, incoming);
    }

    public Task RevokeRefreshTokenAsync(AdminUser admin, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        admin.RefreshTokenHash = null;
        admin.RefreshTokenExpiresAt = null;
        admin.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }
}
