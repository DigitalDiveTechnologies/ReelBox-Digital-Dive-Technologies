using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialReelSaver.Api.Auth;
using SocialReelSaver.Application.Admin.DTOs;
using SocialReelSaver.Application.Admin.UseCases;

namespace SocialReelSaver.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/storage")]
[Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicyAdminOnly)]
public sealed class AdminStorageController(
    GetStorageSummaryUseCase summary,
    ScanStorageOrphansUseCase scan,
    CleanupStorageOrphansUseCase cleanup) : AdminControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken) =>
        Ok(await summary.HandleAsync(cancellationToken));

    [HttpPost("orphan-scan")]
    [Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicyPlatformsManage)]
    public async Task<IActionResult> OrphanScan(CancellationToken cancellationToken) =>
        Ok(await scan.HandleAsync(cancellationToken));

    [HttpPost("cleanup")]
    [Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicyPlatformsManage)]
    public async Task<IActionResult> Cleanup([FromBody] StorageCleanupRequest request, CancellationToken cancellationToken)
    {
        var admin = CurrentAdmin();
        return Ok(await cleanup.HandleAsync(request.Keys ?? [], admin.Id, admin.Email, admin.IpAddress, admin.CorrelationId, cancellationToken));
    }
}
