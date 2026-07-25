namespace SocialReelSaver.Shared.Configuration;

/// <summary>
/// JWT bearer settings (SRS §22 — JWT + refresh tokens).
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Symmetric signing key. Must remain server-side only (SRS §16).
    /// </summary>
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenExpirationMinutes { get; set; } = 15;

    public int RefreshTokenExpirationDays { get; set; } = 7;
}
