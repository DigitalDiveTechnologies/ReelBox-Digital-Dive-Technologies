using SocialReelSaver.Application.Abstractions.Authentication;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Auth.DTOs;
using SocialReelSaver.Application.Common.Exceptions;

namespace SocialReelSaver.Application.Auth.UseCases;

public sealed class ResetUserPasswordUseCase
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwords;

    public ResetUserPasswordUseCase(IUserRepository users, IPasswordHasher passwords)
    {
        _users = users;
        _passwords = passwords;
    }

    public async Task<MessageResponse> HandleAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var otp = request.Otp.Trim();
        var user = await _users.GetByEmailAsync(email, cancellationToken);

        if (user is null ||
            !user.IsActive ||
            string.IsNullOrWhiteSpace(user.PasswordResetOtpHash) ||
            user.PasswordResetOtpExpiresAt is null ||
            user.PasswordResetOtpExpiresAt < DateTimeOffset.UtcNow ||
            !_passwords.VerifyPassword(otp, user.PasswordResetOtpHash))
        {
            throw new UnauthorizedAppException("Invalid or expired reset code.");
        }

        user.PasswordHash = _passwords.HashPassword(request.NewPassword);
        user.PasswordResetOtpHash = null;
        user.PasswordResetOtpExpiresAt = null;
        user.RefreshTokenHash = null;
        user.RefreshTokenExpiresAt = null;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _users.UpdateAsync(user, cancellationToken);
        await _users.SaveChangesAsync(cancellationToken);

        return new MessageResponse("Password updated. You can sign in with your new password.");
    }
}
