namespace SocialReelSaver.Shared.Configuration;

public sealed class ProvidersOptions
{
    public const string SectionName = "Providers";

    /// <summary>
    /// Per-provider resolve timeout (NFR-003).
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Hard ceiling for provider execution including validation overhead.
    /// </summary>
    public int MaximumExecutionSeconds { get; set; } = 60;

    /// <summary>
    /// Meta Graph API base (official host only — SRS §16 SSRF defense).
    /// </summary>
    public string GraphApiBaseUrl { get; set; } = "https://graph.facebook.com/v21.0";

    /// <summary>
    /// Optional Meta app/user access token for Graph lookups (server-side only — SRS §16).
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Host suffixes allowed for resolved downloadable media URLs (SRS §16).
    /// </summary>
    public string[] AllowedResolvedHostSuffixes { get; set; } =
    [
        ".cdninstagram.com",
        ".fbcdn.net",
        "scontent.cdninstagram.com",
        "scontent.xx.fbcdn.net",
        "video.xx.fbcdn.net",
        "video.cdninstagram.com",
    ];

    public ProviderPlatformOptions Instagram { get; set; } = new();

    public ProviderPlatformOptions Facebook { get; set; } = new();
}

public sealed class ProviderPlatformOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// When false, temporary provider failures are treated as permanent for retry policy.
    /// </summary>
    public bool RetryEligible { get; set; } = true;
}
