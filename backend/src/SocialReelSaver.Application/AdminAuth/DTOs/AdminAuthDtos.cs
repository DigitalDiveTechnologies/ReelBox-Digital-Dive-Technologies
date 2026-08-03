namespace SocialReelSaver.Application.AdminAuth.DTOs;

public sealed record AdminLoginRequest(string Email, string Password);

public sealed record AdminRefreshRequest(string RefreshToken);

public sealed record AdminForgotPasswordRequest(string Email);

public sealed record AdminResetPasswordRequest(string Email, string Otp, string NewPassword);

public sealed record AdminMessageResponse(string Message);

public sealed record AdminProfileResponse(
    Guid Id,
    string Email,
    string? DisplayName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record AdminTokensResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds);

public sealed record AdminAuthResponse(
    AdminProfileResponse Admin,
    AdminTokensResponse Tokens);
