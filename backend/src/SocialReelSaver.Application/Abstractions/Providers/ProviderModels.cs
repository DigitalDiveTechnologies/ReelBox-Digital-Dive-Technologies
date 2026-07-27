using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Application.Abstractions.Providers;

public sealed class ProviderCapabilities
{
    public bool CanResolve { get; init; } = true;

    public bool SupportsCancellation { get; init; } = true;

    public bool SupportsTimeout { get; init; } = true;

    /// <summary>
    /// True when the adapter is registered but real extraction is not available yet (SRS FR-007 deferred).
    /// </summary>
    public bool IsPlaceholderImplementation { get; init; }

    public static ProviderCapabilities Placeholder(MediaPlatform platform) => new()
    {
        CanResolve = true,
        SupportsCancellation = true,
        SupportsTimeout = true,
        IsPlaceholderImplementation = true,
    };

    public static ProviderCapabilities ProductionReady() => new()
    {
        CanResolve = true,
        SupportsCancellation = true,
        SupportsTimeout = true,
        IsPlaceholderImplementation = false,
    };
}

public sealed record ProviderContext
{
    public required Guid MediaId { get; init; }

    public required Guid JobId { get; init; }

    public required Guid UserId { get; init; }

    public required MediaPlatform Platform { get; init; }

    public required string OriginalUrl { get; init; }

    public int Attempt { get; init; }

    public string CorrelationId { get; init; } = Guid.NewGuid().ToString("N");

    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record ProviderDiagnostics
{
    public string? ProviderName { get; init; }

    public MediaPlatform Platform { get; init; }

    public string CorrelationId { get; init; } = string.Empty;

    public Guid MediaId { get; init; }

    public Guid JobId { get; init; }

    public DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset CompletedAt { get; init; }

    public TimeSpan Duration => CompletedAt - StartedAt;

    public bool TimedOut { get; init; }

    public bool Cancelled { get; init; }

    public ProviderErrorCode? ErrorCode { get; init; }

    public string? Message { get; init; }

    public bool IsPlaceholder { get; init; }
}

public sealed record ProviderResult
{
    public bool Success { get; init; }

    public bool IsPlaceholder { get; init; }

    public string? ResolvedSourceUrl { get; init; }

    /// <summary>
    /// Optional local file already downloaded by the provider (e.g. yt-dlp).
    /// When set, the pipeline skips the HTTP downloader step.
    /// </summary>
    public string? LocalFilePath { get; init; }

    public string? Title { get; init; }

    public string? SuggestedMimeType { get; init; }

    public string? SuggestedExtension { get; init; }

    public long? SuggestedDurationMs { get; init; }

    public ProviderErrorCode ErrorCode { get; init; } = ProviderErrorCode.None;

    public string? ErrorMessage { get; init; }

    public string? MediaErrorCode =>
        ErrorCode == ProviderErrorCode.None
            ? null
            : ProviderErrorMapper.ToMediaErrorCode(ErrorCode);

    public ProviderDiagnostics? Diagnostics { get; init; }

    public static ProviderResult Ok(
        string resolvedSourceUrl,
        string? title = null,
        string? mimeType = null,
        string? extension = null,
        long? durationMs = null,
        string? localFilePath = null) => new()
    {
        Success = true,
        ResolvedSourceUrl = resolvedSourceUrl,
        LocalFilePath = localFilePath,
        Title = title,
        SuggestedMimeType = mimeType,
        SuggestedExtension = extension,
        SuggestedDurationMs = durationMs,
    };

    public static ProviderResult Placeholder(string providerName, string? title = null) => new()
    {
        Success = false,
        IsPlaceholder = true,
        Title = title ?? $"[{providerName} pending extraction]",
        SuggestedMimeType = "video/mp4",
        SuggestedExtension = ".mp4",
        ErrorCode = ProviderErrorCode.NotImplemented,
        ErrorMessage = $"{providerName} returned placeholder metadata; real source extraction is not available yet.",
    };

    public static ProviderResult Failed(ProviderErrorCode code, string message) => new()
    {
        Success = false,
        ErrorCode = code,
        ErrorMessage = message,
    };
}

public sealed record ProviderExecutionOutcome
{
    public required ProviderResult Result { get; init; }

    public required ProviderDiagnostics Diagnostics { get; init; }

    public IMediaProvider? Provider { get; init; }
}
