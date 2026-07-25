using SocialReelSaver.Application.Abstractions.Authentication;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Auth.DTOs;
using SocialReelSaver.Application.Auth.Mappings;
using SocialReelSaver.Application.Common.Exceptions;

namespace SocialReelSaver.Application.Auth.UseCases;

public sealed class LoginUserUseCase
{
    private readonly IUserAuthenticationService _authService;
    private readonly IUserRepository _users;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;

    public LoginUserUseCase(
        IUserAuthenticationService authService,
        IUserRepository users,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService)
    {
        _authService = authService;
        _users = users;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<AuthResponse> HandleAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _authService.ValidateCredentialsAsync(request.Email, request.Password, cancellationToken);
        if (user is null)
        {
            throw new UnauthorizedAppException("Invalid email or password.");
        }

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
