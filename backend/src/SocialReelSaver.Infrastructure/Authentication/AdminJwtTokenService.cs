using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SocialReelSaver.Application.Abstractions.Authentication;
using SocialReelSaver.Domain.Enums;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Infrastructure.Authentication;

public sealed class AdminJwtTokenService : IAdminJwtTokenService
{
    public const string TokenTypeClaim = "typ";
    public const string TokenTypeValue = "admin";

    private readonly AdminJwtOptions _options;
    private readonly SymmetricSecurityKey _signingKey;

    public AdminJwtTokenService(IOptions<AdminJwtOptions> options)
    {
        _options = options.Value;
        if (string.IsNullOrWhiteSpace(_options.SigningKey) || _options.SigningKey.Length < 32)
        {
            throw new InvalidOperationException(
                "AdminJwt:SigningKey must be configured with at least 32 characters.");
        }

        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
    }

    public Task<string> CreateAccessTokenAsync(
        Guid adminId,
        string email,
        AdminRole role,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var expires = GetAccessTokenExpiry();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, adminId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, adminId.ToString()),
            new(ClaimTypes.Role, role.ToString()),
            new("role", role.ToString()),
            new(TokenTypeClaim, TokenTypeValue),
        };

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
    }

    public Task<string> CreateRefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Task.FromResult(Base64UrlEncode(bytes));
    }

    public DateTimeOffset GetAccessTokenExpiry() =>
        DateTimeOffset.UtcNow.AddMinutes(_options.AccessTokenExpirationMinutes);

    public DateTimeOffset GetRefreshTokenExpiry() =>
        DateTimeOffset.UtcNow.AddDays(_options.RefreshTokenExpirationDays);

    public int GetAccessTokenExpiresInSeconds() =>
        Math.Max(60, _options.AccessTokenExpirationMinutes * 60);

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
