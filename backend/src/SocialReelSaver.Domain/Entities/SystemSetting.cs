namespace SocialReelSaver.Domain.Entities;

/// <summary>
/// Runtime operational setting persisted for admin overlay (no Flutter schema impact).
/// </summary>
public sealed class SystemSetting
{
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string Category { get; set; } = "general";

    public DateTimeOffset UpdatedAt { get; set; }

    public Guid? UpdatedByAdminId { get; set; }
}
