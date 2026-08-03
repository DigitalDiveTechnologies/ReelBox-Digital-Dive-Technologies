using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialReelSaver.Api.Auth;
using SocialReelSaver.Application.AdminAuth.DTOs;
using SocialReelSaver.Application.AdminAuth.UseCases;

namespace SocialReelSaver.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/auth")]
public sealed class AdminAuthController : ControllerBase
{
    private readonly LoginAdminUseCase _login;
    private readonly RefreshAdminTokenUseCase _refresh;
    private readonly LogoutAdminUseCase _logout;
    private readonly GetCurrentAdminUseCase _me;

    public AdminAuthController(
        LoginAdminUseCase login,
        RefreshAdminTokenUseCase refresh,
        LogoutAdminUseCase logout,
        GetCurrentAdminUseCase me)
    {
        _login = login;
        _refresh = refresh;
        _logout = logout;
        _me = me;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AdminAuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] AdminLoginRequest request,
        IValidator<AdminLoginRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var result = await _login.HandleAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AdminAuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] AdminRefreshRequest request,
        IValidator<AdminRefreshRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var result = await _refresh.HandleAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicyAdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var adminId = GetAdminId();
        await _logout.HandleAsync(adminId, cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = AdminAuthConstants.Scheme, Policy = AdminAuthConstants.PolicyAdminOnly)]
    [ProducesResponseType(typeof(AdminProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var adminId = GetAdminId();
        var result = await _me.HandleAsync(adminId, cancellationToken);
        return Ok(result);
    }

    private Guid GetAdminId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (raw is null || !Guid.TryParse(raw, out var adminId))
        {
            throw new UnauthorizedAccessException("Authenticated admin id is missing.");
        }

        return adminId;
    }
}
