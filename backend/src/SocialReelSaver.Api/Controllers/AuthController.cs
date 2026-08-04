using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialReelSaver.Application.Auth.DTOs;
using SocialReelSaver.Application.Auth.UseCases;

namespace SocialReelSaver.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly RegisterUserUseCase _register;
    private readonly LoginUserUseCase _login;
    private readonly RefreshTokenUseCase _refresh;
    private readonly LogoutUserUseCase _logout;
    private readonly GetCurrentUserUseCase _getCurrentUser;
    private readonly ForgotUserPasswordUseCase _forgotPassword;
    private readonly ResetUserPasswordUseCase _resetPassword;

    public AuthController(
        RegisterUserUseCase register,
        LoginUserUseCase login,
        RefreshTokenUseCase refresh,
        LogoutUserUseCase logout,
        GetCurrentUserUseCase getCurrentUser,
        ForgotUserPasswordUseCase forgotPassword,
        ResetUserPasswordUseCase resetPassword)
    {
        _register = register;
        _login = login;
        _refresh = refresh;
        _logout = logout;
        _getCurrentUser = getCurrentUser;
        _forgotPassword = forgotPassword;
        _resetPassword = resetPassword;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        IValidator<RegisterRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var result = await _register.HandleAsync(request, cancellationToken);
        return Created("/api/v1/auth/me", result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        IValidator<LoginRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var result = await _login.HandleAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequest request,
        IValidator<RefreshRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var result = await _refresh.HandleAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        IValidator<ForgotPasswordRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var result = await _forgotPassword.HandleAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        IValidator<ResetPasswordRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var result = await _resetPassword.HandleAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await _logout.HandleAsync(userId, cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var result = await _getCurrentUser.HandleAsync(userId, cancellationToken);
        return Ok(result);
    }

    private Guid GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (raw is null || !Guid.TryParse(raw, out var userId))
        {
            throw new UnauthorizedAccessException("Authenticated user id is missing.");
        }

        return userId;
    }
}
