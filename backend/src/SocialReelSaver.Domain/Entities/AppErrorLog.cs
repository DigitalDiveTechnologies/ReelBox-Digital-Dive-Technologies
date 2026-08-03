namespace SocialReelSaver.Domain.Entities;

/// <summary>
/// Application error / exception log for admin diagnostics.
/// </summary>
public sealed class AppErrorLog
{
    public Guid Id { get; set; }

    public string Level { get; set; } = "Error";

    public string Message { get; set; } = string.Empty;

    public string? Detail { get; set; }

    public string? Source { get; set; }

    public string? CorrelationId { get; set; }

    public string? Path { get; set; }

    public int? StatusCode { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
