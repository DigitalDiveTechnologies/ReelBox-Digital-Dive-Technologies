using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialReelSaver.Api.Auth;
using SocialReelSaver.Application.Admin.DTOs;
using SocialReelSaver.Application.Admin.UseCases;

namespace SocialReelSaver.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/settings")]
[Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicyAdminOnly)]
public sealed class AdminSettingsController(
    GetSettingsAdminUseCase get,
    UpsertSettingsAdminUseCase upsert) : AdminControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        Ok(await get.HandleAsync(cancellationToken));

    [HttpPut]
    [Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicySettingsManage)]
    public async Task<IActionResult> Put([FromBody] UpsertSettingsRequest request, CancellationToken cancellationToken)
    {
        var admin = CurrentAdmin();
        return Ok(await upsert.HandleAsync(request.Settings ?? new Dictionary<string, string>(), admin.Id, admin.Email, admin.IpAddress, admin.CorrelationId, cancellationToken));
    }
}
