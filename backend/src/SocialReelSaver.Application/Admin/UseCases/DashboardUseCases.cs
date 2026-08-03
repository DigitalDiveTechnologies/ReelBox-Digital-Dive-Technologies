using SocialReelSaver.Application.Abstractions.Admin;
using SocialReelSaver.Application.Admin.DTOs;

namespace SocialReelSaver.Application.Admin.UseCases;

public sealed class GetDashboardSummaryUseCase(IAdminMetricsReader metrics)
{
    public Task<DashboardSummaryResponse> HandleAsync(CancellationToken cancellationToken = default) =>
        metrics.GetSummaryAsync(cancellationToken);
}

public sealed class GetDashboardTrendsUseCase(IAdminMetricsReader metrics)
{
    public Task<DashboardTrendsResponse> HandleAsync(int days, CancellationToken cancellationToken = default) =>
        metrics.GetTrendsAsync(Math.Clamp(days, 1, 90), cancellationToken);
}

public sealed class GetDashboardActivityUseCase(IAdminMetricsReader metrics)
{
    public Task<DashboardActivityResponse> HandleAsync(int limit, CancellationToken cancellationToken = default) =>
        metrics.GetActivityAsync(Math.Clamp(limit, 1, 100), cancellationToken);
}
