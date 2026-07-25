namespace SocialReelSaver.Application.Abstractions.Media;

public interface IRetryPolicy
{
    int MaxRetries { get; }

    bool IsRetryable(string? errorCode);

    bool CanRetry(int currentRetryCount, string? errorCode);

    TimeSpan GetBackoffDelay(int retryCountAfterFailure);
}
