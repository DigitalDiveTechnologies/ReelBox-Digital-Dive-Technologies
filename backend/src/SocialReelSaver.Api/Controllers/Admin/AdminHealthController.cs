using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialReelSaver.Api.Auth;
using SocialReelSaver.Application.Admin.UseCases;

namespace SocialReelSaver.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/health")]
[Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicyAdminOnly)]
public sealed class AdminHealthController(GetSystemHealthOverviewUseCase overview) : AdminControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken cancellationToken) =>
        Ok(await overview.HandleAsync(cancellationToken));
}
