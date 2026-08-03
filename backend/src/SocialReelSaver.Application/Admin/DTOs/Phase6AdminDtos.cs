using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Application.Admin.DTOs;

public sealed record AdminMediaListItem(
    Guid Id,
    Guid UserId,
    string? UserEmail,
    string Platform,
    string Status,
    string OriginalUrl,
    string? Title,
    long? FileSizeBytes,
    int RetryCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? ErrorCode);

public sealed record AdminMediaDetailResponse(
    Guid Id,
    Guid UserId,
    string? UserEmail,
    string Platform,
    string Status,
    string OriginalUrl,
    string? NormalizedUrl,
    string? Title,
    string? ThumbnailStorageKey,
    string? MediaStorageKey,
    string? MimeType,
    long? FileSizeBytes,
    long? DurationMs,
    short? ProgressPercent,
    int RetryCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DownloadStartedAt,
    DateTimeOffset? DownloadedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? NextRetryAt,
    string? ErrorCode,
    string? ErrorMessage);

public sealed record UpdatePlatformRequest(bool? Enabled, int? DailyLimit, bool? MaintenanceMode);

public sealed record PlatformAdminItem(
    string Platform,
    bool Enabled,
    bool MaintenanceMode,
    int DailyLimit,
    string Status);

public sealed record UpdateProviderRequest(int? TimeoutSeconds, int? Priority, bool? Enabled);

public sealed record ProviderAdminItem(
    string Name,
    string Platform,
    bool Enabled,
    int TimeoutSeconds,
    int Priority,
    string Resolver,
    string Health,
    bool HasAccessToken,
    bool HasRapidApiKey,
    bool RetryEligible);

public sealed record StorageSummaryResponse(
    string ProviderName,
    int MediaCount,
    long TotalBytes,
    int? OrphanEstimate);

public sealed record OrphanScanResponse(
    bool Supported,
    string? Message,
    IReadOnlyList<string> OrphanKeys,
    int FileCount,
    int DbKeyCount);

public sealed record StorageCleanupRequest(IReadOnlyList<string> Keys);

public sealed record StorageCleanupResponse(
    bool Supported,
    string? Message,
    int DeletedCount,
    IReadOnlyList<string> DeletedKeys);

public sealed record DownloadsTrendsResponse(IReadOnlyList<DashboardTrendPoint> Items);

public sealed record UserActivityPoint(string Date, int NewUsers, int Downloads);

public sealed record UserActivityResponse(IReadOnlyList<UserActivityPoint> Items);

public sealed record PlatformStatItem(string Platform, int Total, int Completed, int Failed, decimal SuccessRate);

public sealed record PlatformStatsResponse(IReadOnlyList<PlatformStatItem> Items);

public sealed record ProviderPerformanceItem(string Platform, int Success, int Fail, decimal SuccessRate);

public sealed record ProviderPerformanceResponse(IReadOnlyList<ProviderPerformanceItem> Items);

public sealed record HealthComponentStatus(string Name, string Status, string? Detail = null);

public sealed record SystemHealthOverviewResponse(
    string OverallStatus,
    IReadOnlyList<HealthComponentStatus> Components);

public sealed record AppErrorLogListItem(
    Guid Id,
    string Level,
    string Message,
    string? Source,
    string? CorrelationId,
    string? Path,
    int? StatusCode,
    DateTimeOffset CreatedAt);

public sealed record AppErrorLogDetailResponse(
    Guid Id,
    string Level,
    string Message,
    string? Detail,
    string? Source,
    string? CorrelationId,
    string? Path,
    int? StatusCode,
    DateTimeOffset CreatedAt);

public sealed record SettingItemResponse(string Key, string Value, string Category);

public sealed record SettingsGroupedResponse(IReadOnlyDictionary<string, IReadOnlyList<SettingItemResponse>> Groups);

public sealed record UpsertSettingsRequest(IReadOnlyDictionary<string, string> Settings);

public sealed record JobStatusCountsResponse(
    int Queued,
    int Active,
    int Completed,
    int Failed,
    int Total);

public static class AdminJobStatusGroups
{
    public static readonly MediaStatus[] Queued = [MediaStatus.Queued, MediaStatus.Preparing];
    public static readonly MediaStatus[] Active = [MediaStatus.Downloading, MediaStatus.Processing];
    public static readonly MediaStatus[] Completed = [MediaStatus.Completed];
    public static readonly MediaStatus[] Failed = [MediaStatus.Failed];

    public static IReadOnlyList<MediaStatus>? Resolve(string? statusGroup) =>
        statusGroup?.Trim().ToLowerInvariant() switch
        {
            "queued" => Queued,
            "active" => Active,
            "completed" => Completed,
            "failed" => Failed,
            "all" or null or "" => null,
            _ => null,
        };
}
