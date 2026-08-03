using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Domain.Entities;

/// <summary>
/// Administrator identity — isolated from mobile <see cref="User"/>.
/// </summary>
public sealed class AdminUser
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public AdminRole Role { get; set; } = AdminRole.Analyst;

    public string? RefreshTokenHash { get; set; }

    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
