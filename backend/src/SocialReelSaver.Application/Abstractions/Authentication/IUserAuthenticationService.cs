using SocialReelSaver.Domain.Entities;

namespace SocialReelSaver.Application.Abstractions.Authentication;

public interface IUserAuthenticationService
{
    Task<User?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
