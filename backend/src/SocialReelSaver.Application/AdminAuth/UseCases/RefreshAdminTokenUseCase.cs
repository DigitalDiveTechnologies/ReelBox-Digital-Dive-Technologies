using SocialReelSaver.Application.Abstractions.Authentication;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.AdminAuth.DTOs;
using SocialReelSaver.Application.AdminAuth.Mappings;
using SocialReelSaver.Application.Common.Exceptions;

namespace SocialReelSaver.Application.AdminAuth.UseCases;

public sealed class RefreshAdminTokenUseCase
{
    private readonly IAdminUserRepository _admins;
    private readonly IAdminJwtTokenService _jwt;
    private readonly IAdminRefreshTokenService _refresh;

    public RefreshAdminTokenUseCase(
        IAdminUserRepository admins,
        IAdminJwtTokenService jwt,
        IAdminRefreshTokenService refresh)
    {
        _admins = admins;
        _jwt = jwt;
        _refresh = refresh;
    }

    public async Task<AdminAuthResponse> HandleAsync(
        AdminRefreshRequest request,
        CancellationToken cancellationToken = default)
    {
        var hash = _refresh.HashToken(request.RefreshToken);
        var admin = await _admins.GetByRefreshTokenHashAsync(hash, cancellationToken);
        if (admin is null || !admin.IsActive)
        {
            throw new UnauthorizedAppException("Invalid or expired refresh token.");
        }

        if (!_refresh.ValidateRefreshToken(admin, request.RefreshToken))
        {
            throw new UnauthorizedAppException("Invalid or expired refresh token.");
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
