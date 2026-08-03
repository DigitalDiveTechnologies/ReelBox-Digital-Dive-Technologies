using SocialReelSaver.Application.Abstractions.Authentication;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.AdminAuth.DTOs;
using SocialReelSaver.Application.AdminAuth.Mappings;
using SocialReelSaver.Application.Common.Exceptions;

namespace SocialReelSaver.Application.AdminAuth.UseCases;

public sealed class LoginAdminUseCase
{
    private readonly IAdminAuthenticationService _auth;
    private readonly IAdminUserRepository _admins;
    private readonly IAdminJwtTokenService _jwt;
    private readonly IAdminRefreshTokenService _refresh;

    public LoginAdminUseCase(
        IAdminAuthenticationService auth,
        IAdminUserRepository admins,
        IAdminJwtTokenService jwt,
        IAdminRefreshTokenService refresh)
    {
        _auth = auth;
        _admins = admins;
        _jwt = jwt;
        _refresh = refresh;
    }

    public async Task<AdminAuthResponse> HandleAsync(
        AdminLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var admin = await _auth.ValidateCredentialsAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (admin is null)
        {
            throw new UnauthorizedAppException("Invalid email or password.");
        }

        var accessToken = await _jwt.CreateAccessTokenAsync(
            admin.Id,
            admin.Email,
            admin.Role,
            cancellationToken);
        var refreshToken = await _jwt.CreateRefreshTokenAsync(cancellationToken);
        var refreshExpiry = _jwt.GetRefreshTokenExpiry();

        await _refresh.StoreRefreshTokenAsync(admin, refreshToken, refreshExpiry, cancellationToken);
        await _admins.UpdateAsync(admin, cancellationToken);
        await _admins.SaveChangesAsync(cancellationToken);

        return admin.ToAuthResponse(
            accessToken,
            refreshToken,
            _jwt.GetAccessTokenExpiresInSeconds());
    }
}
