using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialReelSaver.Api.Auth;
using SocialReelSaver.Application.Admin.DTOs;
using SocialReelSaver.Application.Admin.UseCases;

namespace SocialReelSaver.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/roles")]
[Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicyAdminOnly)]
public sealed class AdminRolesController(ListRolesUseCase listRoles, AssignAdminRoleUseCase assignRole) : AdminControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) => Ok(await listRoles.HandleAsync(cancellationToken));

    [HttpPatch("admins/{id:guid}")]
    [Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicySuperAdmin)]
    public async Task<IActionResult> Assign(Guid id, [FromBody] UpdateAdminRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Role)) return BadRequest("Role is required.");
        var admin = CurrentAdmin();
        return Ok(await assignRole.HandleAsync(id, request.Role, admin.Id, admin.Email, admin.IpAddress, admin.CorrelationId, cancellationToken));
    }
}
