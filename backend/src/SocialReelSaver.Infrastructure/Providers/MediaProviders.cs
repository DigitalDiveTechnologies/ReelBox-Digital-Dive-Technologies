using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialReelSaver.Application.Abstractions.Admin;
using SocialReelSaver.Application.Abstractions.Providers;
using SocialReelSaver.Domain.Enums;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Infrastructure.Providers;

public sealed class InstagramProvider : IMediaProvider
{
    private readonly MetaGraphMediaResolver _metaResolver;
    private readonly YtDlpMediaResolver _ytDlpResolver;
    private readonly ProvidersOptions _options;

    public InstagramProvider(
        MetaGraphMediaResolver metaResolver,
        YtDlpMediaResolver ytDlpResolver,
        IOptions<ProvidersOptions> options)
    {
        _metaResolver = metaResolver;
        _ytDlpResolver = ytDlpResolver;
        _options = options.Value;
    }

    public string Name => nameof(InstagramProvider);

    public MediaPlatform Platform => MediaPlatform.Instagram;

    public ProviderCapabilities Capabilities { get; } = ProviderCapabilities.ProductionReady();

    public Task<ProviderResult> ExecuteAsync(
        ProviderContext context,
        CancellationToken cancellationToken = default)
    {
        return UseYtDlp
            ? _ytDlpResolver.ResolveAsync(Platform, context.OriginalUrl, context.MediaId, cancellationToken)
            : _metaResolver.ResolveAsync(Platform, context.OriginalUrl, cancellationToken);
    }

    private bool UseYtDlp =>
        !string.Equals(_options.Resolver, "MetaGraph", StringComparison.OrdinalIgnoreCase);
}

public sealed class FacebookProvider : IMediaProvider
{
    private readonly MetaGraphMediaResolver _metaResolver;
    private readonly YtDlpMediaResolver _ytDlpResolver;
    private readonly ProvidersOptions _options;

    public FacebookProvider(
        MetaGraphMediaResolver metaResolver,
        YtDlpMediaResolver ytDlpResolver,
        IOptions<ProvidersOptions> options)
    {
        _metaResolver = metaResolver;
        _ytDlpResolver = ytDlpResolver;
        _options = options.Value;
    }

    public string Name => nameof(FacebookProvider);

    public MediaPlatform Platform => MediaPlatform.Facebook;

    public ProviderCapabilities Capabilities { get; } = ProviderCapabilities.ProductionReady();

    public Task<ProviderResult> ExecuteAsync(
        ProviderContext context,
        CancellationToken cancellationToken = default)
    {
        return UseYtDlp
            ? _ytDlpResolver.ResolveAsync(Platform, context.OriginalUrl, context.MediaId, cancellationToken)
            : _metaResolver.ResolveAsync(Platform, context.OriginalUrl, cancellationToken);
    }

    private bool UseYtDlp =>
        !string.Equals(_options.Resolver, "MetaGraph", StringComparison.OrdinalIgnoreCase);
}

public sealed class MediaProviderFactory : IMediaProviderFactory
{
    private readonly IReadOnlyDictionary<MediaPlatform, IMediaProvider> _providers;
    private readonly ProvidersOptions _options;
    private readonly IOperationalSettings _operational;

    public MediaProviderFactory(
        IEnumerable<IMediaProvider> providers,
        IOptions<ProvidersOptions> options,
        IOperationalSettings operational)
    {
        _providers = providers.ToDictionary(p => p.Platform);
        _options = options.Value;
        _operational = operational;
    }

    public IMediaProvider Create(MediaPlatform platform)
    {
        if (!TryCreate(platform, out var provider) || provider is null)
        {
            throw new ProviderException(
                ProviderErrorCode.UnsupportedPlatform,
                $"No media provider registered for platform '{platform}'.",
                providerName: null);
        }

        return provider;
    }

    public bool TryCreate(MediaPlatform platform, out IMediaProvider? provider)
    {
        if (!_providers.TryGetValue(platform, out provider))
        {
            provider = null;
            return false;
        }

        if (!IsEnabled(platform))
        {
            provider = null;
            return false;
        }

        return true;
    }

    private bool IsEnabled(MediaPlatform platform)
    {
        // Best-effort load; factory is singleton and must not block forever.
        _ = _operational.EnsureLoadedAsync();

        if (_operational.GetBool(OperationalSettingKeys.PlatformMaintenanceMode, false)
            || _operational.GetBool(OperationalSettingKeys.SettingsMaintenanceMode, false))
        {
            return false;
        }

        var configEnabled = GetPlatformOptions(platform).Enabled;
        var key = platform switch
        {
            MediaPlatform.Instagram => OperationalSettingKeys.PlatformInstagramEnabled,
            MediaPlatform.Facebook => OperationalSettingKeys.PlatformFacebookEnabled,
            _ => null,
        };

        if (key is null) return false;

        var providerEnabledKey = platform switch
        {
            MediaPlatform.Instagram => OperationalSettingKeys.ProviderInstagramEnabled,
            MediaPlatform.Facebook => OperationalSettingKeys.ProviderFacebookEnabled,
            _ => null,
        };

        var platformEnabled = _operational.GetBool(key, configEnabled);
        var providerEnabled = providerEnabledKey is null || _operational.GetBool(providerEnabledKey, true);
        return platformEnabled && providerEnabled;
    }

    internal ProviderPlatformOptions GetPlatformOptions(MediaPlatform platform) =>
        platform switch
        {
            MediaPlatform.Instagram => _options.Instagram,
            MediaPlatform.Facebook => _options.Facebook,
            _ => new ProviderPlatformOptions { Enabled = false, RetryEligible = false },
        };
}

public sealed class MediaProviderResolver : IMediaProviderResolver
{
    private readonly IMediaProviderFactory _factory;

    public MediaProviderResolver(IMediaProviderFactory factory)
    {
        _factory = factory;
    }

    public IMediaProvider Resolve(MediaPlatform platform) => _factory.Create(platform);

    public bool TryResolve(MediaPlatform platform, out IMediaProvider? provider) =>
        _factory.TryCreate(platform, out provider);
}

public sealed class ProviderResultValidator : IProviderResultValidator
{
    private readonly MetaGraphMediaResolver _resolver;

    public ProviderResultValidator(
        MetaGraphMediaResolver resolver,
        IOptions<ProvidersOptions> options)
    {
        _ = options;
        _resolver = resolver;
    }

    public ProviderResult Validate(ProviderResult result, IMediaProvider provider)
    {
        if (!result.Success)
        {
            return result;
        }

        // Provider already materialized a local file (yt-dlp path) — skip CDN URL checks.
        if (!string.IsNullOrWhiteSpace(result.LocalFilePath) && File.Exists(result.LocalFilePath))
        {
            return result;
        }

        if (string.IsNullOrWhiteSpace(result.ResolvedSourceUrl))
        {
            return ProviderResult.Failed(
                ProviderErrorCode.InvalidProviderResponse,
                $"{provider.Name} returned success without a resolved source URL.");
        }

        if (!Uri.TryCreate(result.ResolvedSourceUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return ProviderResult.Failed(
                ProviderErrorCode.InvalidProviderResponse,
                $"{provider.Name} returned a non-HTTP(S) resolved source URL.");
        }

        // yt-dlp returns a local file; MetaGraph returns CDN URLs checked against allowlist.
        if (!_resolver.IsAllowedResolvedHost(result.ResolvedSourceUrl))
        {
            return ProviderResult.Failed(
                ProviderErrorCode.AccessNotPermitted,
                $"{provider.Name} resolved a media host outside the approved CDN allowlist.");
        }

        return result;
    }
}

public sealed class MediaProviderExecutor : IMediaProviderExecutor
{
    private readonly IMediaProviderResolver _resolver;
    private readonly IProviderResultValidator _resultValidator;
    private readonly ProvidersOptions _options;
    private readonly IOperationalSettings _operational;
    private readonly ILogger<MediaProviderExecutor> _logger;

    public MediaProviderExecutor(
        IMediaProviderResolver resolver,
        IProviderResultValidator resultValidator,
        IOptions<ProvidersOptions> options,
        IOperationalSettings operational,
        ILogger<MediaProviderExecutor> logger)
    {
        _resolver = resolver;
        _resultValidator = resultValidator;
        _options = options.Value;
        _operational = operational;
        _logger = logger;
    }

    public async Task<ProviderExecutionOutcome> ExecuteAsync(
        ProviderContext context,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;

        if (!_resolver.TryResolve(context.Platform, out var provider) || provider is null)
        {
            var disabled = IsConfiguredButDisabled(context.Platform);
            var code = disabled
                ? ProviderErrorCode.ConfigurationError
                : ProviderErrorCode.UnsupportedPlatform;
            var message = disabled
                ? $"Provider for platform '{context.Platform}' is disabled."
                : $"No provider available for platform '{context.Platform}'.";

            var failed = ProviderResult.Failed(code, message);
            var diagnostics = BuildDiagnostics(context, null, startedAt, failed.ErrorCode, message, timedOut: false, cancelled: false, isPlaceholder: false);
            LogFailure(diagnostics);
            return new ProviderExecutionOutcome { Result = failed with { Diagnostics = diagnostics }, Diagnostics = diagnostics };
        }

        if (!provider.Capabilities.CanResolve)
        {
            var failed = ProviderResult.Failed(
                ProviderErrorCode.ProviderUnavailable,
                $"{provider.Name} cannot resolve media sources.");
            var diagnostics = BuildDiagnostics(context, provider, startedAt, failed.ErrorCode, failed.ErrorMessage, false, false, provider.Capabilities.IsPlaceholderImplementation);
            LogFailure(diagnostics);
            return new ProviderExecutionOutcome { Result = failed with { Diagnostics = diagnostics }, Diagnostics = diagnostics, Provider = provider };
        }

        _logger.LogInformation(
            "Provider {ProviderName} selected for media {MediaId} job {JobId} correlation {CorrelationId} attempt {Attempt}",
            provider.Name,
            context.MediaId,
            context.JobId,
            context.CorrelationId,
            context.Attempt);

        _ = _operational.EnsureLoadedAsync();
        var configuredTimeout = _operational.GetInt(OperationalSettingKeys.ProviderTimeoutSeconds, _options.TimeoutSeconds);
        var timeoutSeconds = Math.Max(1, Math.Min(configuredTimeout, _options.MaximumExecutionSeconds));
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        ProviderResult result;
        var timedOut = false;
        var cancelled = false;

        try
        {
            result = await provider.ExecuteAsync(context, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            cancelled = true;
            result = ProviderResult.Failed(ProviderErrorCode.ProviderCancelled, $"{provider.Name} execution was cancelled.");
        }
        catch (OperationCanceledException)
        {
            timedOut = true;
            result = ProviderResult.Failed(ProviderErrorCode.ProviderTimeout, $"{provider.Name} execution timed out after {timeoutSeconds}s.");
        }
        catch (ProviderException ex)
        {
            result = ProviderResult.Failed(ex.ErrorCode, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled provider exception from {ProviderName} for media {MediaId}",
                provider.Name,
                context.MediaId);
            result = ProviderResult.Failed(ProviderErrorCode.TemporaryFailure, $"{provider.Name} failed unexpectedly.");
        }

        if (result.Success)
        {
            result = _resultValidator.Validate(result, provider);
        }

        if (result.IsPlaceholder || provider.Capabilities.IsPlaceholderImplementation)
        {
            result = result with
            {
                IsPlaceholder = true,
                Success = false,
                ErrorCode = result.ErrorCode == ProviderErrorCode.None
                    ? ProviderErrorCode.NotImplemented
                    : result.ErrorCode,
                ErrorMessage = result.ErrorMessage
                    ?? $"{provider.Name} is a placeholder implementation.",
            };
        }

        var platformOptions = GetPlatformOptions(context.Platform);
        if (!platformOptions.RetryEligible &&
            ProviderErrorMapper.IsRetryEligibleByDefault(result.ErrorCode))
        {
            result = result with
            {
                ErrorCode = ProviderErrorCode.PermanentFailure,
                ErrorMessage = result.ErrorMessage + " (provider retry disabled by configuration).",
            };
        }

        var completedDiagnostics = BuildDiagnostics(
            context,
            provider,
            startedAt,
            result.ErrorCode == ProviderErrorCode.None ? null : result.ErrorCode,
            result.ErrorMessage,
            timedOut,
            cancelled,
            result.IsPlaceholder || provider.Capabilities.IsPlaceholderImplementation);

        result = result with { Diagnostics = completedDiagnostics };

        if (result.Success)
        {
            _logger.LogInformation(
                "Provider {ProviderName} resolved media {MediaId} in {DurationMs}ms correlation {CorrelationId}",
                provider.Name,
                context.MediaId,
                completedDiagnostics.Duration.TotalMilliseconds,
                context.CorrelationId);
        }
        else
        {
            LogFailure(completedDiagnostics);
        }

        return new ProviderExecutionOutcome
        {
            Result = result,
            Diagnostics = completedDiagnostics,
            Provider = provider,
        };
    }

    private bool IsConfiguredButDisabled(MediaPlatform platform)
    {
        if (platform is not (MediaPlatform.Instagram or MediaPlatform.Facebook))
            return false;

        _ = _operational.EnsureLoadedAsync();
        if (_operational.GetBool(OperationalSettingKeys.PlatformMaintenanceMode, false)
            || _operational.GetBool(OperationalSettingKeys.SettingsMaintenanceMode, false))
            return true;

        var key = platform == MediaPlatform.Instagram
            ? OperationalSettingKeys.PlatformInstagramEnabled
            : OperationalSettingKeys.PlatformFacebookEnabled;
        return !_operational.GetBool(key, GetPlatformOptions(platform).Enabled);
    }

    private ProviderPlatformOptions GetPlatformOptions(MediaPlatform platform) =>
        platform switch
        {
            MediaPlatform.Instagram => _options.Instagram,
            MediaPlatform.Facebook => _options.Facebook,
            _ => new ProviderPlatformOptions { Enabled = false, RetryEligible = false },
        };

    private void LogFailure(ProviderDiagnostics diagnostics) =>
        _logger.LogWarning(
            "Provider {ProviderName} failed for media {MediaId} job {JobId} code={ErrorCode} timeout={TimedOut} cancelled={Cancelled} durationMs={DurationMs} correlation={CorrelationId}: {Message}",
            diagnostics.ProviderName,
            diagnostics.MediaId,
            diagnostics.JobId,
            diagnostics.ErrorCode,
            diagnostics.TimedOut,
            diagnostics.Cancelled,
            diagnostics.Duration.TotalMilliseconds,
            diagnostics.CorrelationId,
            diagnostics.Message);

    private static ProviderDiagnostics BuildDiagnostics(
        ProviderContext context,
        IMediaProvider? provider,
        DateTimeOffset startedAt,
        ProviderErrorCode? errorCode,
        string? message,
        bool timedOut,
        bool cancelled,
        bool isPlaceholder) =>
        new()
        {
            ProviderName = provider?.Name,
            Platform = context.Platform,
            CorrelationId = context.CorrelationId,
            MediaId = context.MediaId,
            JobId = context.JobId,
            StartedAt = startedAt,
            CompletedAt = DateTimeOffset.UtcNow,
            TimedOut = timedOut,
            Cancelled = cancelled,
            ErrorCode = errorCode,
            Message = message,
            IsPlaceholder = isPlaceholder,
        };
}
