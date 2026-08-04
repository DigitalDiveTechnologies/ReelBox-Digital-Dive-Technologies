namespace SocialReelSaver.Domain.Entities;

/// <summary>
/// In-app notification for a mobile user (download completion, account alerts, etc.).
/// </summary>
public sealed class Notification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    /// <summary>Optional related media item (e.g. completed download).</summary>
    public Guid? MediaId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
