using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Application.Abstractions.Providers;

/// <summary>
/// Platform-specific media source resolver (SRS FR-007 / FR-018 / §11).
/// </summary>
public interface IMediaProvider
{
    string Name { get; }

    MediaPlatform Platform { get; }

    ProviderCapabilities Capabilities { get; }

    /// <summary>
    /// Executes provider resolution for the given context.
    /// Implementations must honor cancellation and must not perform unofficial scraping.
    /// </summary>
    Task<ProviderResult> ExecuteAsync(
        ProviderContext context,
        CancellationToken cancellationToken = default);
}

public interface IMediaProviderFactory
{
    IMediaProvider Create(MediaPlatform platform);

    bool TryCreate(MediaPlatform platform, out IMediaProvider? provider);
}

public interface IMediaProviderResolver
{
    /// <summary>
    /// Selects a registered provider for the platform (SRS §11 step 3).
    /// </summary>
    IMediaProvider Resolve(MediaPlatform platform);

    bool TryResolve(MediaPlatform platform, out IMediaProvider? provider);
}

public interface IProviderResultValidator
{
    ProviderResult Validate(ProviderResult result, IMediaProvider provider);
}

/// <summary>
/// Orchestrates provider selection, validation, timed execution, and result validation.
/// </summary>
public interface IMediaProviderExecutor
{
    Task<ProviderExecutionOutcome> ExecuteAsync(
        ProviderContext context,
        CancellationToken cancellationToken = default);
}
