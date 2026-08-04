namespace SocialReelSaver.Application.Auth.DTOs;

public sealed record RegisterRequest(string Email, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Email, string Otp, string NewPassword);

public sealed record VerifyEmailRequest(string Email, string Otp);

public sealed record ResendSignupOtpRequest(string Email);

public sealed record MessageResponse(string Message);

public sealed record UserResponse(Guid Id, string Email, DateTimeOffset CreatedAt);

public sealed record AuthTokensResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt);

public sealed record AuthResponse(UserResponse User, AuthTokensResponse Tokens);
