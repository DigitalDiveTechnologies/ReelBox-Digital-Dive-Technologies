using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptionsAccessor) =>
            {
                var jwtOptions = jwtOptionsAccessor.Value;

                bearerOptions.RequireHttpsMetadata = false;
                bearerOptions.SaveToken = true;
                bearerOptions.MapInboundClaims = false;
                bearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = CreateSigningKey(jwtOptions.SigningKey),
                    NameClaimType = "sub",
                    RoleClaimType = "role",
                };

                bearerOptions.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Prefer standard Bearer; fall back to X-Access-Token when
                        // the host already consumes Authorization for IIS Basic Auth.
                        var header = context.Request.Headers.Authorization.FirstOrDefault();
                        if (!string.IsNullOrWhiteSpace(header)
                            && header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Token = header["Bearer ".Length..].Trim();
                        }
                        else if (context.Request.Headers.TryGetValue("X-Access-Token", out var values))
                        {
                            var token = values.FirstOrDefault();
                            if (!string.IsNullOrWhiteSpace(token))
                            {
                                context.Token = token.Trim();
                            }
                        }

                        return Task.CompletedTask;
                    },
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/problem+json";
                        var payload = JsonSerializer.Serialize(new
                        {
                            type = "https://httpstatuses.com/401",
                            title = "Unauthorized",
                            status = 401,
                            detail = "Authentication is required.",
                        });
                        await context.Response.WriteAsync(payload);
                    },
                };
            });

        services.AddAuthorization();

        return services;
    }

    private static SymmetricSecurityKey CreateSigningKey(string signingKey)
    {
        var keyBytes = string.IsNullOrWhiteSpace(signingKey)
            ? Encoding.UTF8.GetBytes("DEV_ONLY_INSECURE_PLACEHOLDER_KEY_32CHARS!!")
            : Encoding.UTF8.GetBytes(signingKey);

        return new SymmetricSecurityKey(keyBytes);
    }
}
