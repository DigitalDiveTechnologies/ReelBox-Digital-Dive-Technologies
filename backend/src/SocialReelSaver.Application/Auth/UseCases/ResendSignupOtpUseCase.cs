using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Auth.DTOs;

namespace SocialReelSaver.Application.Auth.UseCases;

public sealed class ResendSignupOtpUseCase
{
    private readonly IUserRepository _users;
    private readonly RegisterUserUseCase _register;

    public ResendSignupOtpUseCase(IUserRepository users, RegisterUserUseCase register)
    {
        _users = users;
        _register = register;
    }

    public async Task<MessageResponse> HandleAsync(
        ResendSignupOtpRequest request,
        CancellationToken cancellationToken = default)
    {
        const string genericMessage =
            "If an unverified account exists for that email, a new code has been sent.";

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _users.GetByEmailAsync(email, cancellationToken);

        if (user is null || !user.IsActive || user.EmailVerified)
        {
            return new MessageResponse(genericMessage);
        }

        await _register.AssignAndSendOtpAsync(user, cancellationToken);
        await _users.UpdateAsync(user, cancellationToken);
        await _users.SaveChangesAsync(cancellationToken);

        return new MessageResponse(genericMessage);
    }
}
