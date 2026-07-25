using SocialReelSaver.Application.Abstractions.Authentication;
using SocialReelSaver.Application.Auth.DTOs;
using SocialReelSaver.Application.Auth.Mappings;
using SocialReelSaver.Application.Common.Exceptions;

namespace SocialReelSaver.Application.Auth.UseCases;

public sealed class GetCurrentUserUseCase
{
    private readonly IUserAuthenticationService _authService;

    public GetCurrentUserUseCase(IUserAuthenticationService authService)
    {
        _authService = authService;
    }

    public async Task<UserResponse> HandleAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _authService.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new NotFoundException("User not found.");
        }

        return user.ToResponse();
    }
}
