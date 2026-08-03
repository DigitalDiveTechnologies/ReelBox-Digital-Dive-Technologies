using SocialReelSaver.Application.Abstractions.Authentication;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Domain.Entities;

namespace SocialReelSaver.Infrastructure.Authentication;

public sealed class AdminAuthenticationService : IAdminAuthenticationService
{
    private readonly IAdminUserRepository _admins;
    private readonly IPasswordHasher _passwordHasher;

    public AdminAuthenticationService(
        IAdminUserRepository admins,
        IPasswordHasher passwordHasher)
    {
        _admins = admins;
        _passwordHasher = passwordHasher;
    }

    public async Task<AdminUser?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var admin = await _admins.GetByEmailAsync(normalized, cancellationToken);
        if (admin is null || !admin.IsActive)
        {
            return null;
        }

        if (!_passwordHasher.VerifyPassword(password, admin.PasswordHash))
        {
            return null;
        }

        return admin;
    }
}
