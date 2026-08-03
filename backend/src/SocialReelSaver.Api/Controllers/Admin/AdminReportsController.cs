using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialReelSaver.Api.Auth;
using SocialReelSaver.Application.Admin.UseCases;

namespace SocialReelSaver.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/reports")]
[Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicyAdminOnly)]
public sealed class AdminReportsController(
    GetDownloadsTrendsUseCase downloads,
    GetUserActivityReportUseCase userActivity,
    GetPlatformStatsUseCase platforms,
    GetProviderPerformanceUseCase providers,
    ExportReportCsvUseCase export) : AdminControllerBase
{
    [HttpGet("downloads-trends")]
    public async Task<IActionResult> DownloadsTrends([FromQuery] int days = 14, CancellationToken cancellationToken = default) =>
        Ok(await downloads.HandleAsync(days, cancellationToken));

    [HttpGet("user-activity")]
    public async Task<IActionResult> UserActivity([FromQuery] int days = 14, CancellationToken cancellationToken = default) =>
        Ok(await userActivity.HandleAsync(days, cancellationToken));

    [HttpGet("platform-stats")]
    public async Task<IActionResult> PlatformStats(CancellationToken cancellationToken) =>
        Ok(await platforms.HandleAsync(cancellationToken));

    [HttpGet("provider-performance")]
    public async Task<IActionResult> ProviderPerformance(CancellationToken cancellationToken) =>
        Ok(await providers.HandleAsync(cancellationToken));

    [HttpGet("export.csv")]
    public async Task<IActionResult> ExportCsv([FromQuery] string type = "downloads", CancellationToken cancellationToken = default)
    {
        var csv = await export.HandleAsync(type, cancellationToken);
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"report-{type}.csv");
    }
}
