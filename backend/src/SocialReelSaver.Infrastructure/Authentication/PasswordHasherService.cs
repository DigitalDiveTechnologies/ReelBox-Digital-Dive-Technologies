using Microsoft.AspNetCore.Identity;
using SocialReelSaver.Application.Abstractions.Authentication;

namespace SocialReelSaver.Infrastructure.Authentication;

public sealed class PasswordHasherService : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();
    private readonly object _user = new();

    public string HashPassword(string password) =>
        _hasher.HashPassword(_user, password);

    public bool VerifyPassword(string password, string passwordHash)
    {
        var result = _hasher.VerifyHashedPassword(_user, passwordHash, password);
        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
