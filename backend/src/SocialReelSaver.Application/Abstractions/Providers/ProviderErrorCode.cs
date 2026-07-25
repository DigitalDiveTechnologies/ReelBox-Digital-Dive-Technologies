using SocialReelSaver.Application.Media.Errors;

namespace SocialReelSaver.Application.Abstractions.Providers;

/// <summary>
/// Structured provider failure categories mapped to SRS §14 media error codes.
/// </summary>
public enum ProviderErrorCode
{
    None = 0,
    UnsupportedPlatform,
    ProviderUnavailable,
    ProviderTimeout,
    ProviderCancelled,
    TemporaryFailure,
    PermanentFailure,
    InvalidProviderResponse,
    ConfigurationError,
    NotImplemented,
    AccessNotPermitted,
    MediaNotFound,
}

public static class ProviderErrorMapper
{
    /// <summary>
    /// Maps provider failures to persisted/public media <c>error_code</c> values (SRS §14 only).
    /// </summary>
    public static string ToMediaErrorCode(ProviderErrorCode code) => code switch
    {
        ProviderErrorCode.UnsupportedPlatform => SrsMediaErrorCodes.UnsupportedPlatform,
        ProviderErrorCode.ProviderUnavailable => SrsMediaErrorCodes.ProviderTemporaryFailure,
        ProviderErrorCode.ProviderTimeout => SrsMediaErrorCodes.DownloadTimeout,
        ProviderErrorCode.ProviderCancelled => SrsMediaErrorCodes.Unknown,
        ProviderErrorCode.TemporaryFailure => SrsMediaErrorCodes.ProviderTemporaryFailure,
        ProviderErrorCode.PermanentFailure => SrsMediaErrorCodes.Unknown,
        ProviderErrorCode.InvalidProviderResponse => SrsMediaErrorCodes.Unknown,
        ProviderErrorCode.ConfigurationError => SrsMediaErrorCodes.Unknown,
        ProviderErrorCode.NotImplemented => SrsMediaErrorCodes.Unknown,
        ProviderErrorCode.AccessNotPermitted => SrsMediaErrorCodes.AccessNotPermitted,
        ProviderErrorCode.MediaNotFound => SrsMediaErrorCodes.MediaNotFound,
        _ => SrsMediaErrorCodes.Unknown,
    };

    public static bool IsRetryEligibleByDefault(ProviderErrorCode code) => code switch
    {
        ProviderErrorCode.ProviderUnavailable => true,
        ProviderErrorCode.ProviderTimeout => true,
        ProviderErrorCode.TemporaryFailure => true,
        _ => false,
    };
}
