using SocialReelSaver.Application.Abstractions.Authentication;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Auth.DTOs;
using SocialReelSaver.Application.Auth.Mappings;
using SocialReelSaver.Application.Common.Exceptions;

namespace SocialReelSaver.Application.Auth.UseCases;

public sealed class RefreshTokenUseCase
{
    private readonly IUserRepository _users;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;

    public RefreshTokenUseCase(
        IUserRepository users,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService)
    {
        _users = users;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<AuthResponse> HandleAsync(
        RefreshRequest request,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = _refreshTokenService.HashToken(request.RefreshToken);
        var user = await _users.GetByRefreshTokenHashAsync(tokenHash, cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAppException("Invalid or expired refresh token.");
        }

        if (!_refreshTokenService.ValidateRefreshToken(user, request.RefreshToken))
        {
            throw new UnauthorizedAppException("Invalid or expired refresh token.");
        }

        // Rotation: replace previous refresh token with a new one.
        var accessToken = await _jwtTokenService.CreateAccessTokenAsync(user.Id, user.Email, cancellationToken);
        var refreshToken = await _jwtTokenService.CreateRefreshTokenAsync(cancellationToken);
        var refreshExpiry = _jwtTokenService.GetRefreshTokenExpiry();

        await _refreshTokenService.StoreRefreshTokenAsync(user, refreshToken, refreshExpiry, cancellationToken);
        await _users.UpdateAsync(user, cancellationToken);
        await _users.SaveChangesAsync(cancellationToken);

        return user.ToAuthResponse(
            accessToken,
            refreshToken,
            _jwtTokenService.GetAccessTokenExpiry(),
            refreshExpiry);
    }
}
