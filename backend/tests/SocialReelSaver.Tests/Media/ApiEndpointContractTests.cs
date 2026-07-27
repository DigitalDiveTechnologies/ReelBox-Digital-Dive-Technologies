using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using SocialReelSaver.Api.Controllers;

namespace SocialReelSaver.Tests.Media;

/// <summary>
/// Locks SRS §9 media API surface (plus §22 JWT auth supporting routes).
/// </summary>
public sealed class ApiEndpointContractTests
{
    [Fact]
    public void MediaController_ExposesSrsSection9Endpoints()
    {
        var routes = GetActionRoutes(typeof(MediaController));

        Assert.Contains(("POST", ""), routes);
        Assert.Contains(("GET", ""), routes);
        Assert.Contains(("GET", "{id:guid}"), routes);
        Assert.Contains(("POST", "{id:guid}/retry"), routes);
        Assert.Contains(("DELETE", "{id:guid}"), routes);
        Assert.Contains(("GET", "{id:guid}/playback"), routes);
    }

    [Fact]
    public void MediaController_RequiresAuthorization()
    {
        Assert.NotNull(typeof(MediaController).GetCustomAttribute<AuthorizeAttribute>());
    }

    [Fact]
    public void AuthController_ExposesJwtLifecycleEndpoints()
    {
        var routes = GetActionRoutes(typeof(AuthController));

        Assert.Contains(("POST", "register"), routes);
        Assert.Contains(("POST", "login"), routes);
        Assert.Contains(("POST", "refresh"), routes);
        Assert.Contains(("POST", "logout"), routes);
        Assert.Contains(("GET", "me"), routes);
    }

    private static HashSet<(string Method, string Template)> GetActionRoutes(Type controllerType)
    {
        var result = new HashSet<(string, string)>();

        foreach (var method in controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
        {
            var http = method.GetCustomAttributes()
                .OfType<HttpMethodAttribute>()
                .FirstOrDefault();
            if (http is null)
            {
                continue;
            }

            var verb = http.HttpMethods.First();
            var template = http.Template ?? string.Empty;
            result.Add((verb, template));
        }

        return result;
    }
}
