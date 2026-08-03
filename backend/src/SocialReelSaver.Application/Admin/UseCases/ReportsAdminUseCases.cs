using SocialReelSaver.Application.Abstractions.Admin;
using SocialReelSaver.Application.Admin.DTOs;

namespace SocialReelSaver.Application.Admin.UseCases;

public sealed class GetDownloadsTrendsUseCase(IAdminMetricsReader metrics)
{
    public Task<DownloadsTrendsResponse> HandleAsync(int days, CancellationToken cancellationToken = default) =>
        metrics.GetDownloadsTrendsAsync(Math.Clamp(days, 1, 90), cancellationToken);
}

public sealed class GetUserActivityReportUseCase(IAdminMetricsReader metrics)
{
    public Task<UserActivityResponse> HandleAsync(int days, CancellationToken cancellationToken = default) =>
        metrics.GetUserActivityAsync(Math.Clamp(days, 1, 90), cancellationToken);
}

public sealed class GetPlatformStatsUseCase(IAdminMetricsReader metrics)
{
    public Task<PlatformStatsResponse> HandleAsync(CancellationToken cancellationToken = default) =>
        metrics.GetPlatformStatsAsync(cancellationToken);
}

public sealed class GetProviderPerformanceUseCase(IAdminMetricsReader metrics)
{
    public Task<ProviderPerformanceResponse> HandleAsync(CancellationToken cancellationToken = default) =>
        metrics.GetProviderPerformanceAsync(cancellationToken);
}

public sealed class ExportReportCsvUseCase(IAdminMetricsReader metrics)
{
    public Task<string> HandleAsync(string type, CancellationToken cancellationToken = default) =>
        metrics.ExportCsvAsync(type, cancellationToken);
}
