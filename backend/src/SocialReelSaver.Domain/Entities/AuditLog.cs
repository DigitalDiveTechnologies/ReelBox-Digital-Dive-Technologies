namespace SocialReelSaver.Domain.Entities;

/// <summary>
/// Append-only privileged admin action record (tech spec §10.1).
/// </summary>
public sealed class AuditLog
{
    public Guid Id { get; set; }

    public Guid AdminId { get; set; }

    public string AdminEmail { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string? OldValuesJson { get; set; }

    public string? NewValuesJson { get; set; }

    public string? IpAddress { get; set; }

    public string? CorrelationId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
