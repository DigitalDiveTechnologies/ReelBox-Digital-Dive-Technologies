using SocialReelSaver.Application.Admin.DTOs;

namespace SocialReelSaver.Application.Abstractions.Admin;

public interface IAdminMetricsReader
{
    Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<DashboardTrendsResponse> GetTrendsAsync(int days, CancellationToken cancellationToken = default);

    Task<DashboardActivityResponse> GetActivityAsync(int limit, CancellationToken cancellationToken = default);

    Task<DownloadsTrendsResponse> GetDownloadsTrendsAsync(int days, CancellationToken cancellationToken = default);

    Task<UserActivityResponse> GetUserActivityAsync(int days, CancellationToken cancellationToken = default);

    Task<PlatformStatsResponse> GetPlatformStatsAsync(CancellationToken cancellationToken = default);

    Task<ProviderPerformanceResponse> GetProviderPerformanceAsync(CancellationToken cancellationToken = default);

    Task<string> ExportCsvAsync(string type, CancellationToken cancellationToken = default);
}
