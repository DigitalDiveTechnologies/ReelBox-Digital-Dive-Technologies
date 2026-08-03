using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialReelSaver.Api.Auth;
using SocialReelSaver.Application.Admin.DTOs;
using SocialReelSaver.Application.Admin.UseCases;

namespace SocialReelSaver.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/providers")]
[Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicyAdminOnly)]
public sealed class AdminProvidersController(
    ListProvidersAdminUseCase list,
    UpdateProviderAdminUseCase update,
    ProbeProviderHealthUseCase probe) : AdminControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await list.HandleAsync(cancellationToken));

    [HttpPatch("{name}")]
    [Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicySettingsManage)]
    public async Task<IActionResult> Update(string name, [FromBody] UpdateProviderRequest request, CancellationToken cancellationToken)
    {
        var admin = CurrentAdmin();
        return Ok(await update.HandleAsync(name, request, admin.Id, admin.Email, admin.IpAddress, admin.CorrelationId, cancellationToken));
    }

    [HttpPost("{name}/health-check")]
    [Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicyPlatformsManage)]
    public async Task<IActionResult> HealthCheck(string name, CancellationToken cancellationToken) =>
        Ok(await probe.HandleAsync(name, cancellationToken));
}
