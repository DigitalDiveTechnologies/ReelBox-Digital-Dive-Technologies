namespace SocialReelSaver.Shared.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string ConnectionString { get; set; } = string.Empty;
}

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = string.Empty;
}

public sealed class ObjectStorageOptions
{
    public const string SectionName = "ObjectStorage";

    /// <summary>
    /// Local | S3Compatible | CloudflareR2
    /// </summary>
    public string Provider { get; set; } = "Local";

    public string BucketName { get; set; } = string.Empty;

    public string ServiceUrl { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string Region { get; set; } = "auto";

    /// <summary>
    /// Root folder used by <c>Local</c> provider.
    /// </summary>
    public string LocalRootPath { get; set; } = "storage";

    public int UploadTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Absolute public API base used for local application playback URLs (e.g. https://api.example.com).
    /// </summary>
    public string PublicApiBaseUrl { get; set; } = "http://localhost:5080";

    public int PlaybackUrlExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// HMAC key for local application playback URLs. Falls back to JWT signing key when empty.
    /// </summary>
    public string PlaybackSigningKey { get; set; } = string.Empty;
}

public sealed class DownloadOptions
{
    public const string SectionName = "Download";

    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Maximum accepted media size in bytes (default 250 MB).
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 250L * 1024 * 1024;

    public string TempFolder { get; set; } = "temp/downloads";

    public string[] AllowedMimeTypes { get; set; } =
    [
        "video/mp4",
        "video/quicktime",
        "video/webm",
        "image/jpeg",
        "image/png",
        "image/webp",
        "application/octet-stream",
    ];
}

public sealed class WorkerOptions
{
    public const string SectionName = "Worker";

    public string QueueName { get; set; } = "media-download-jobs";

    public int PollIntervalMilliseconds { get; set; } = 1000;

    public int DequeueTimeoutMilliseconds { get; set; } = 2000;

    public int MaxRetries { get; set; } = 3;

    public int BaseBackoffSeconds { get; set; } = 2;

    public int MaxBackoffSeconds { get; set; } = 60;

    /// <summary>
    /// Stale Downloading/Processing items older than this are reclaimed after Worker crash.
    /// </summary>
    public int StuckJobTimeoutMinutes { get; set; } = 15;
}
