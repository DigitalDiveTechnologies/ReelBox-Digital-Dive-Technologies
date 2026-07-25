namespace SocialReelSaver.Application.Auth.DTOs;

public sealed record RegisterRequest(string Email, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record UserResponse(Guid Id, string Email, DateTimeOffset CreatedAt);

public sealed record AuthTokensResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt);

public sealed record AuthResponse(UserResponse User, AuthTokensResponse Tokens);
