namespace SocialReelSaver.Application.Abstractions.Admin;

public interface IOperationalSettings
{
    Task EnsureLoadedAsync(CancellationToken cancellationToken = default);

    Task RefreshAsync(CancellationToken cancellationToken = default);

    bool GetBool(string key, bool fallback);

    int GetInt(string key, int fallback);

    string GetString(string key, string fallback);

    bool TryGet(string key, out string? value);

    Task SetAsync(string key, string value, string category, Guid? adminId, CancellationToken cancellationToken = default);

    IReadOnlyDictionary<string, string> Snapshot();
}

public static class OperationalSettingKeys
{
    public const string PlatformInstagramEnabled = "platform.instagram.enabled";
    public const string PlatformFacebookEnabled = "platform.facebook.enabled";
    public const string PlatformInstagramDailyLimit = "platform.instagram.dailyLimit";
    public const string PlatformFacebookDailyLimit = "platform.facebook.dailyLimit";
    public const string PlatformMaintenanceMode = "platform.maintenanceMode";
    public const string ProviderTimeoutSeconds = "provider.timeoutSeconds";
    public const string ProviderPriorityInstagram = "provider.priority.instagram";
    public const string ProviderPriorityFacebook = "provider.priority.facebook";
    public const string ProviderInstagramEnabled = "provider.instagram.enabled";
    public const string ProviderFacebookEnabled = "provider.facebook.enabled";
    public const string SettingsMaintenanceMode = "settings.maintenanceMode";

    public static readonly IReadOnlySet<string> Allowlist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        PlatformInstagramEnabled,
        PlatformFacebookEnabled,
        PlatformInstagramDailyLimit,
        PlatformFacebookDailyLimit,
        PlatformMaintenanceMode,
        ProviderTimeoutSeconds,
        ProviderPriorityInstagram,
        ProviderPriorityFacebook,
        ProviderInstagramEnabled,
        ProviderFacebookEnabled,
        SettingsMaintenanceMode,
        "feature.downloadRetry.enabled",
        "feature.playback.enabled",
        "feature.thumbnails.enabled",
        "security.requireActiveUser",
        "security.maxFailedLogins",
        "ops.orphanScan.enabled",
        "ops.cleanup.enabled",
        "app.displayName",
        "app.supportEmail",
    };

    public static string CategoryFor(string key) =>
        key.StartsWith("platform.", StringComparison.OrdinalIgnoreCase) ? "platforms" :
        key.StartsWith("provider.", StringComparison.OrdinalIgnoreCase) ? "providers" :
        key.StartsWith("feature.", StringComparison.OrdinalIgnoreCase) ? "features" :
        key.StartsWith("security.", StringComparison.OrdinalIgnoreCase) ? "security" :
        key.StartsWith("ops.", StringComparison.OrdinalIgnoreCase) ? "ops" :
        key.StartsWith("settings.", StringComparison.OrdinalIgnoreCase) ? "maintenance" :
        key.StartsWith("app.", StringComparison.OrdinalIgnoreCase) ? "app" :
        "general";

    public static bool IsSecretKey(string key)
    {
        var k = key.ToLowerInvariant();
        return k.Contains("secret") || k.Contains("password") || k.Contains("apikey")
            || k.Contains("api_key") || k.Contains("token") || k.Contains("signing");
    }
}
