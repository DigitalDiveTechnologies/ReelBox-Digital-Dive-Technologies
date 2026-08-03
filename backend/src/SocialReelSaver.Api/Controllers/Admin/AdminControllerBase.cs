using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace SocialReelSaver.Api.Controllers.Admin;

public abstract class AdminControllerBase : ControllerBase
{
    protected (Guid Id, string Email, string? IpAddress, string CorrelationId) CurrentAdmin()
    {
        var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(rawId, out var id)) throw new UnauthorizedAccessException("Authenticated admin id is missing.");
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? string.Empty;
        return (id, email, HttpContext.Connection.RemoteIpAddress?.ToString(), HttpContext.TraceIdentifier);
    }
}
