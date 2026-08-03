using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialReelSaver.Api.Auth;
using SocialReelSaver.Application.Admin.UseCases;
using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/media")]
[Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicyAdminOnly)]
public sealed class AdminMediaController(
    ListMediaAdminUseCase list,
    GetMediaAdminUseCase get,
    DeleteMediaAdminUseCase delete,
    RetryMediaAdminUseCase retry,
    GetMediaPlaybackAdminUseCase playback) : AdminControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] MediaStatus? status = null,
        [FromQuery] MediaPlatform? platform = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        CancellationToken cancellationToken = default) =>
        Ok(await list.HandleAsync(page, pageSize, search, status, platform, userId, sortBy, sortDir, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await get.HandleAsync(id, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicyMediaManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var admin = CurrentAdmin();
        await delete.HandleAsync(id, admin.Id, admin.Email, admin.IpAddress, admin.CorrelationId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/retry")]
    [Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicyMediaManage)]
    public async Task<IActionResult> Retry(Guid id, CancellationToken cancellationToken)
    {
        var admin = CurrentAdmin();
        return Ok(await retry.HandleAsync(id, admin.Id, admin.Email, admin.IpAddress, admin.CorrelationId, cancellationToken));
    }

    [HttpGet("{id:guid}/playback")]
    [Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicyMediaManage)]
    public async Task<IActionResult> Playback(Guid id, CancellationToken cancellationToken) =>
        Ok(await playback.HandleAsync(id, cancellationToken));
}
