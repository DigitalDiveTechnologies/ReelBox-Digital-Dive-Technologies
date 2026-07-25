namespace SocialReelSaver.Application.Abstractions.Authentication;

public interface IJwtTokenService
{
    Task<string> CreateAccessTokenAsync(
        Guid userId,
        string email,
        CancellationToken cancellationToken = default);

    Task<string> CreateRefreshTokenAsync(CancellationToken cancellationToken = default);

    DateTimeOffset GetAccessTokenExpiry();

    DateTimeOffset GetRefreshTokenExpiry();
}
