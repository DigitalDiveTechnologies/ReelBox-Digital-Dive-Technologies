using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialReelSaver.Api.Auth;
using SocialReelSaver.Application.Admin.UseCases;

namespace SocialReelSaver.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicyAdminOnly)]
public sealed class AdminDashboardController(GetDashboardSummaryUseCase summary, GetDashboardTrendsUseCase trends, GetDashboardActivityUseCase activity) : AdminControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken) => Ok(await summary.HandleAsync(cancellationToken));

    [HttpGet("trends")]
    public async Task<IActionResult> GetTrends([FromQuery] int days = 14, CancellationToken cancellationToken = default) => Ok(await trends.HandleAsync(days, cancellationToken));

    [HttpGet("activity")]
    public async Task<IActionResult> GetActivity([FromQuery] int limit = 20, CancellationToken cancellationToken = default) => Ok(await activity.HandleAsync(limit, cancellationToken));
}
