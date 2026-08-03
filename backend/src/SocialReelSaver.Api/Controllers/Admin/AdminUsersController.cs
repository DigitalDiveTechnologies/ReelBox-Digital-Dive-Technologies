using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialReelSaver.Api.Auth;
using SocialReelSaver.Application.Admin.DTOs;
using SocialReelSaver.Application.Admin.UseCases;

namespace SocialReelSaver.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/users")]
[Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicyAdminOnly)]
public sealed class AdminUsersController(ListUsersAdminUseCase list, GetUserAdminUseCase get, UpdateUserStatusUseCase updateStatus, RevokeUserSessionsUseCase revoke) : AdminControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? search = null, [FromQuery] bool? isActive = null, [FromQuery] string? sortBy = null, [FromQuery] string? sortDir = null, CancellationToken cancellationToken = default) =>
        Ok(await list.HandleAsync(page, pageSize, search, isActive, sortBy, sortDir, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) => Ok(await get.HandleAsync(id, cancellationToken));

    [HttpPatch("{id:guid}/status")]
    [Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicyUsersManage)]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        var admin = CurrentAdmin();
        await updateStatus.HandleAsync(id, request.IsActive, admin.Id, admin.Email, admin.IpAddress, admin.CorrelationId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/revoke-sessions")]
    [Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicyUsersManage)]
    public async Task<IActionResult> RevokeSessions(Guid id, CancellationToken cancellationToken)
    {
        var admin = CurrentAdmin();
        await revoke.HandleAsync(id, admin.Id, admin.Email, admin.IpAddress, admin.CorrelationId, cancellationToken);
        return NoContent();
    }
}
