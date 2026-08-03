using SocialReelSaver.Application.Abstractions.Authentication;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Common.Exceptions;

namespace SocialReelSaver.Application.AdminAuth.UseCases;

public sealed class LogoutAdminUseCase
{
    private readonly IAdminUserRepository _admins;
    private readonly IAdminRefreshTokenService _refresh;

    public LogoutAdminUseCase(
        IAdminUserRepository admins,
        IAdminRefreshTokenService refresh)
    {
        _admins = admins;
        _refresh = refresh;
    }

    public async Task HandleAsync(Guid adminId, CancellationToken cancellationToken = default)
    {
        var admin = await _admins.GetByIdAsync(adminId, cancellationToken);
        if (admin is null)
        {
            throw new UnauthorizedAppException("Administrator session is invalid.");
        }

        await _refresh.RevokeRefreshTokenAsync(admin, cancellationToken);
        await _admins.UpdateAsync(admin, cancellationToken);
        await _admins.SaveChangesAsync(cancellationToken);
    }
}
