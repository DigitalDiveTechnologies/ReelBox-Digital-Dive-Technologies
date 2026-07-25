using SocialReelSaver.Application.Abstractions.Authentication;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Common.Exceptions;

namespace SocialReelSaver.Application.Auth.UseCases;

public sealed class LogoutUserUseCase
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenService _refreshTokenService;

    public LogoutUserUseCase(IUserRepository users, IRefreshTokenService refreshTokenService)
    {
        _users = users;
        _refreshTokenService = refreshTokenService;
    }

    public async Task HandleAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException("User not found.");
        }

        await _refreshTokenService.RevokeRefreshTokenAsync(user, cancellationToken);
        await _users.UpdateAsync(user, cancellationToken);
        await _users.SaveChangesAsync(cancellationToken);
    }
}
