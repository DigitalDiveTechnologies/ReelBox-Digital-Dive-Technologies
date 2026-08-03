using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialReelSaver.Api.Auth;
using SocialReelSaver.Application.Admin.UseCases;
using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/jobs")]
[Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicyAdminOnly)]
public sealed class AdminJobsController(
    ListJobsAdminUseCase list,
    RetryJobAdminUseCase retry,
    CancelJobAdminUseCase cancel,
    RequeueJobAdminUseCase requeue) : AdminControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? statusGroup = "all",
        [FromQuery] string? search = null,
        [FromQuery] MediaPlatform? platform = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        CancellationToken cancellationToken = default)
    {
        var (pageResult, counts) = await list.HandleAsync(page, pageSize, statusGroup, search, platform, userId, sortBy, sortDir, cancellationToken);
        return Ok(new { pageResult.Items, pageResult.Page, pageResult.PageSize, pageResult.TotalCount, pageResult.TotalPages, counts });
    }

    [HttpPost("{id:guid}/retry")]
    [Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicyMediaManage)]
    public async Task<IActionResult> Retry(Guid id, CancellationToken cancellationToken)
    {
        var admin = CurrentAdmin();
        return Ok(await retry.HandleAsync(id, admin.Id, admin.Email, admin.IpAddress, admin.CorrelationId, cancellationToken));
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicyMediaManage)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var admin = CurrentAdmin();
        await cancel.HandleAsync(id, admin.Id, admin.Email, admin.IpAddress, admin.CorrelationId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/requeue")]
    [Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicyMediaManage)]
    public async Task<IActionResult> Requeue(Guid id, CancellationToken cancellationToken)
    {
        var admin = CurrentAdmin();
        await requeue.HandleAsync(id, admin.Id, admin.Email, admin.IpAddress, admin.CorrelationId, cancellationToken);
        return NoContent();
    }
}
