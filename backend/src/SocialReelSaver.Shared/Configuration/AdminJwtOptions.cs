namespace SocialReelSaver.Shared.Configuration;

/// <summary>
/// Admin-only JWT settings. Independent audience from mobile <see cref="JwtOptions"/>.
/// </summary>
public sealed class AdminJwtOptions
{
    public const string SectionName = "AdminJwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = "SocialReelSaver.Admin";

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenExpirationMinutes { get; set; } = 15;

    public int RefreshTokenExpirationDays { get; set; } = 7;
}

/// <summary>
/// Optional first SuperAdmin seed when <c>admin_users</c> is empty.
/// </summary>
public sealed class AdminBootstrapOptions
{
    public const string SectionName = "AdminBootstrap";

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string DisplayName { get; set; } = "Super Admin";
}
