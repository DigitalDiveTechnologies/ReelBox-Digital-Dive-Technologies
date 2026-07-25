namespace SocialReelSaver.Application.Media.Errors;

/// <summary>
/// Public media error codes exposed via API / persisted <c>error_code</c> (SRS §14 only).
/// </summary>
public static class SrsMediaErrorCodes
{
    public const string InvalidUrl = "INVALID_URL";
    public const string UnsupportedPlatform = "UNSUPPORTED_PLATFORM";
    public const string AccessNotPermitted = "ACCESS_NOT_PERMITTED";
    public const string MediaNotFound = "MEDIA_NOT_FOUND";
    public const string ProviderTemporaryFailure = "PROVIDER_TEMPORARY_FAILURE";
    public const string DownloadTimeout = "DOWNLOAD_TIMEOUT";
    public const string FileTooLarge = "FILE_TOO_LARGE";
    public const string StorageFailure = "STORAGE_FAILURE";
    public const string Unknown = "UNKNOWN";

    private static readonly HashSet<string> PublicCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        InvalidUrl,
        UnsupportedPlatform,
        AccessNotPermitted,
        MediaNotFound,
        ProviderTemporaryFailure,
        DownloadTimeout,
        FileTooLarge,
        StorageFailure,
        Unknown,
    };

    /// <summary>
    /// Internal permanent failures that map to <see cref="Unknown"/> but must not be retried.
    /// </summary>
    private static readonly HashSet<string> PermanentInternalCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "PROVIDER_NOT_IMPLEMENTED",
        "PROVIDER_CANCELLED",
        "INVALID_PROVIDER_RESPONSE",
        "CONFIGURATION_ERROR",
        "INVALID_MEDIA",
        "UNSUPPORTED_MEDIA_TYPE",
        "THUMBNAIL_NOT_IMPLEMENTED",
        "INVALID_STATE",
    };

    /// <summary>
    /// Maps any internal failure code to an SRS §14 public code for API/persistence.
    /// </summary>
    public static string ToPublic(string? internalCode)
    {
        if (string.IsNullOrWhiteSpace(internalCode))
        {
            return Unknown;
        }

        if (PublicCodes.Contains(internalCode))
        {
            return NormalizePublic(internalCode);
        }

        return internalCode.ToUpperInvariant() switch
        {
            "STORAGE_NOT_IMPLEMENTED" => StorageFailure,
            "STORAGE_OBJECT_MISSING" => StorageFailure,
            "STORAGE_OBJECT_EXISTS" => StorageFailure,
            "INVALID_STORAGE_KEY" => StorageFailure,
            "STORAGE_MIME_MISMATCH" => StorageFailure,
            "STORAGE_LENGTH_MISMATCH" => StorageFailure,
            _ => Unknown,
        };
    }

    /// <summary>
    /// Retry eligibility using SRS §14 behavior, while preserving permanent internal failures.
    /// </summary>
    public static bool IsRetryable(string? internalOrPublicCode)
    {
        if (string.IsNullOrWhiteSpace(internalOrPublicCode))
        {
            return false;
        }

        if (PermanentInternalCodes.Contains(internalOrPublicCode))
        {
            return false;
        }

        var publicCode = ToPublic(internalOrPublicCode);
        return publicCode switch
        {
            ProviderTemporaryFailure => true,
            DownloadTimeout => true,
            StorageFailure => true,
            Unknown => true,
            _ => false,
        };
    }

    private static string NormalizePublic(string code) => code.ToUpperInvariant() switch
    {
        "INVALID_URL" => InvalidUrl,
        "UNSUPPORTED_PLATFORM" => UnsupportedPlatform,
        "ACCESS_NOT_PERMITTED" => AccessNotPermitted,
        "MEDIA_NOT_FOUND" => MediaNotFound,
        "PROVIDER_TEMPORARY_FAILURE" => ProviderTemporaryFailure,
        "DOWNLOAD_TIMEOUT" => DownloadTimeout,
        "FILE_TOO_LARGE" => FileTooLarge,
        "STORAGE_FAILURE" => StorageFailure,
        _ => Unknown,
    };
}
