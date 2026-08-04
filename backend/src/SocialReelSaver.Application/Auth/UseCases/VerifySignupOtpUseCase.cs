using SocialReelSaver.Application.Abstractions.Authentication;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Auth.DTOs;
using SocialReelSaver.Application.Auth.Mappings;
using SocialReelSaver.Application.Common.Exceptions;

namespace SocialReelSaver.Application.Auth.UseCases;

public sealed class VerifySignupOtpUseCase
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwords;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;

    public VerifySignupOtpUseCase(
        IUserRepository users,
        IPasswordHasher passwords,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService)
    {
        _users = users;
        _passwords = passwords;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<AuthResponse> HandleAsync(
        VerifyEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var otp = request.Otp.Trim();
        var user = await _users.GetByEmailAsync(email, cancellationToken);

        if (user is null ||
            !user.IsActive ||
            user.EmailVerified ||
            string.IsNullOrWhiteSpace(user.EmailVerificationOtpHash) ||
            user.EmailVerificationOtpExpiresAt is null ||
            user.EmailVerificationOtpExpiresAt < DateTimeOffset.UtcNow ||
            !_passwords.VerifyPassword(otp, user.EmailVerificationOtpHash))
        {
            throw new UnauthorizedAppException("Invalid or expired verification code.");
        }

        user.EmailVerified = true;
        user.EmailVerificationOtpHash = null;
        user.EmailVerificationOtpExpiresAt = null;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        var accessToken = await _jwtTokenService.CreateAccessTokenAsync(user.Id, user.Email, cancellationToken);
        var refreshToken = await _jwtTokenService.CreateRefreshTokenAsync(cancellationToken);
        var refreshExpiry = _jwtTokenService.GetRefreshTokenExpiry();

        await _refreshTokenService.StoreRefreshTokenAsync(user, refreshToken, refreshExpiry, cancellationToken);
        await _users.UpdateAsync(user, cancellationToken);
        await _users.SaveChangesAsync(cancellationToken);

        return user.ToAuthResponse(
            accessToken,
            refreshToken,
            _jwtTokenService.GetAccessTokenExpiry(),
            refreshExpiry);
    }
}
