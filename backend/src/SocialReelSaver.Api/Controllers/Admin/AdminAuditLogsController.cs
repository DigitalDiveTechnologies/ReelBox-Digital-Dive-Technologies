using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialReelSaver.Api.Auth;
using SocialReelSaver.Application.Admin.UseCases;

namespace SocialReelSaver.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/audit-logs")]
[Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicyAdminOnly)]
public sealed class AdminAuditLogsController(ListAuditLogsUseCase list, GetAuditLogUseCase get) : AdminControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? search = null, [FromQuery] Guid? adminId = null, [FromQuery] string? action = null, [FromQuery] DateTimeOffset? fromUtc = null, [FromQuery] DateTimeOffset? toUtc = null, [FromQuery] string? sortBy = null, [FromQuery] string? sortDir = null, CancellationToken cancellationToken = default) =>
        Ok(await list.HandleAsync(page, pageSize, search, adminId, action, fromUtc, toUtc, sortBy, sortDir, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) => Ok(await get.HandleAsync(id, cancellationToken));
}
