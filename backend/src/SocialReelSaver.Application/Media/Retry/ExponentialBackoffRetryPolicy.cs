using Microsoft.Extensions.Options;
using SocialReelSaver.Application.Abstractions.Media;
using SocialReelSaver.Application.Media.Errors;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Application.Media.Retry;

public sealed class ExponentialBackoffRetryPolicy : IRetryPolicy
{
    private readonly WorkerOptions _options;

    public ExponentialBackoffRetryPolicy(IOptions<WorkerOptions> options)
    {
        _options = options.Value;
    }

    public int MaxRetries => _options.MaxRetries;

    public bool IsRetryable(string? errorCode) => SrsMediaErrorCodes.IsRetryable(errorCode);

    public bool CanRetry(int currentRetryCount, string? errorCode) =>
        IsRetryable(errorCode) && currentRetryCount < MaxRetries;

    public TimeSpan GetBackoffDelay(int retryCountAfterFailure)
    {
        var attempt = Math.Max(1, retryCountAfterFailure);
        var seconds = _options.BaseBackoffSeconds * Math.Pow(2, attempt - 1);
        seconds = Math.Min(seconds, _options.MaxBackoffSeconds);
        return TimeSpan.FromSeconds(seconds);
    }
}
