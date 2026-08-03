using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialReelSaver.Api.Auth;
using SocialReelSaver.Application.Admin.DTOs;
using SocialReelSaver.Application.Admin.UseCases;

namespace SocialReelSaver.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/platforms")]
[Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicyAdminOnly)]
public sealed class AdminPlatformsController(
    ListPlatformsAdminUseCase list,
    UpdatePlatformAdminUseCase update) : AdminControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await list.HandleAsync(cancellationToken));

    [HttpPatch("{platform}")]
    [Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicyPlatformsManage)]
    public async Task<IActionResult> Update(string platform, [FromBody] UpdatePlatformRequest request, CancellationToken cancellationToken)
    {
        var admin = CurrentAdmin();
        return Ok(await update.HandleAsync(platform, request, admin.Id, admin.Email, admin.IpAddress, admin.CorrelationId, cancellationToken));
    }
}
