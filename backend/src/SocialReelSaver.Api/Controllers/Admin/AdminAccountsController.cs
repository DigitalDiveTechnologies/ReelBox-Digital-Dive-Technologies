using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialReelSaver.Api.Auth;
using SocialReelSaver.Application.Admin.DTOs;
using SocialReelSaver.Application.Admin.UseCases;

namespace SocialReelSaver.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/admins")]
[Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicyAdminOnly)]
public sealed class AdminAccountsController(ListAdminAccountsUseCase list, GetAdminAccountUseCase get, CreateAdminAccountUseCase create, UpdateAdminAccountUseCase update) : AdminControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? search = null, [FromQuery] string? role = null, [FromQuery] bool? isActive = null, [FromQuery] string? sortBy = null, [FromQuery] string? sortDir = null, CancellationToken cancellationToken = default) =>
        Ok(await list.HandleAsync(page, pageSize, search, role, isActive, sortBy, sortDir, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) => Ok(await get.HandleAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicySuperAdmin)]
    public async Task<IActionResult> Create(CreateAdminRequest request, CancellationToken cancellationToken)
    {
        var admin = CurrentAdmin();
        var result = await create.HandleAsync(request, admin.Id, admin.Email, admin.IpAddress, admin.CorrelationId, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicySuperAdmin)]
    public async Task<IActionResult> Update(Guid id, UpdateAdminRequest request, CancellationToken cancellationToken)
    {
        var admin = CurrentAdmin();
        return Ok(await update.HandleAsync(id, request, admin.Id, admin.Email, admin.IpAddress, admin.CorrelationId, cancellationToken));
    }
}
