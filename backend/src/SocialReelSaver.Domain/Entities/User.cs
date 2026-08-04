namespace SocialReelSaver.Domain.Entities;

/// <summary>
/// Authenticated application user (SRS NFR-005 / §22).
/// </summary>
public sealed class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? RefreshTokenHash { get; set; }

    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }

    /// <summary>Hashed 6-digit password-reset OTP (cleared after use / expiry).</summary>
    public string? PasswordResetOtpHash { get; set; }

    public DateTimeOffset? PasswordResetOtpExpiresAt { get; set; }

    /// <summary>False until signup email OTP is verified. Existing users migrate as true.</summary>
    public bool EmailVerified { get; set; }

    /// <summary>Hashed 6-digit signup verification OTP (cleared after use / expiry).</summary>
    public string? EmailVerificationOtpHash { get; set; }

    public DateTimeOffset? EmailVerificationOtpExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<MediaItem> MediaItems { get; set; } = new List<MediaItem>();
}
