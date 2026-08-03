using SocialReelSaver.Application.Abstractions.Admin;
using SocialReelSaver.Application.Abstractions.Authentication;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.AdminAuth.DTOs;
using SocialReelSaver.Application.Common.Exceptions;

namespace SocialReelSaver.Application.AdminAuth.UseCases;

public sealed class ResetAdminPasswordUseCase
{
    private readonly IAdminUserRepository _admins;
    private readonly IPasswordHasher _passwords;
    private readonly IAuditLogWriter _audit;

    public ResetAdminPasswordUseCase(
        IAdminUserRepository admins,
        IPasswordHasher passwords,
        IAuditLogWriter audit)
    {
        _admins = admins;
        _passwords = passwords;
        _audit = audit;
    }

    public async Task<AdminMessageResponse> HandleAsync(
        AdminResetPasswordRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var otp = request.Otp.Trim();
        var admin = await _admins.GetByEmailAsync(email, cancellationToken);

        if (admin is null ||
            !admin.IsActive ||
            string.IsNullOrWhiteSpace(admin.PasswordResetOtpHash) ||
            admin.PasswordResetOtpExpiresAt is null ||
            admin.PasswordResetOtpExpiresAt < DateTimeOffset.UtcNow ||
            !_passwords.VerifyPassword(otp, admin.PasswordResetOtpHash))
        {
            throw new UnauthorizedAppException("Invalid or expired reset code.");
        }

        admin.PasswordHash = _passwords.HashPassword(request.NewPassword);
        admin.PasswordResetOtpHash = null;
        admin.PasswordResetOtpExpiresAt = null;
        admin.RefreshTokenHash = null;
        admin.RefreshTokenExpiresAt = null;
        admin.UpdatedAt = DateTimeOffset.UtcNow;

        await _admins.UpdateAsync(admin, cancellationToken);
        await _admins.SaveChangesAsync(cancellationToken);

        await _audit.WriteAsync(
            admin.Id,
            admin.Email,
            "admin.password_reset",
            "AdminUser",
            admin.Id.ToString(),
            oldValues: null,
            newValues: new { passwordChanged = true },
            ipAddress,
            correlationId: null,
            cancellationToken);

        return new AdminMessageResponse("Password updated. You can sign in with your new password.");
    }
}
