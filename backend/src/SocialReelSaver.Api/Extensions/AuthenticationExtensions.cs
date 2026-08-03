using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SocialReelSaver.Api.Auth;
using SocialReelSaver.Domain.Enums;
using SocialReelSaver.Infrastructure.Authentication;
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
        services.Configure<AdminJwtOptions>(configuration.GetSection(AdminJwtOptions.SectionName));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer()
            .AddJwtBearer(AdminAuthConstants.Scheme);

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptionsAccessor) =>
            {
                ConfigureBearer(
                    bearerOptions,
                    jwtOptionsAccessor.Value.Issuer,
                    jwtOptionsAccessor.Value.Audience,
                    jwtOptionsAccessor.Value.SigningKey);
            });

        services.AddOptions<JwtBearerOptions>(AdminAuthConstants.Scheme)
            .Configure<IOptions<AdminJwtOptions>>((bearerOptions, adminJwtAccessor) =>
            {
                var adminJwt = adminJwtAccessor.Value;
                ConfigureBearer(
                    bearerOptions,
                    adminJwt.Issuer,
                    adminJwt.Audience,
                    adminJwt.SigningKey);

                var existingOnTokenValidated = bearerOptions.Events.OnTokenValidated;
                bearerOptions.Events.OnTokenValidated = async context =>
                {
                    if (existingOnTokenValidated is not null)
                    {
                        await existingOnTokenValidated(context);
                    }

                    var typ = context.Principal?.FindFirst(AdminJwtTokenService.TokenTypeClaim)?.Value;
                    if (!string.Equals(typ, AdminJwtTokenService.TokenTypeValue, StringComparison.Ordinal))
                    {
                        context.Fail("Token is not an admin access token.");
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminAuthConstants.PolicyAdminOnly, policy =>
            {
                policy.AddAuthenticationSchemes(AdminAuthConstants.Scheme);
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(
                    AdminJwtTokenService.TokenTypeClaim,
                    AdminJwtTokenService.TokenTypeValue);
            });

            AddAdminRolePolicy(options, AdminAuthConstants.PolicySuperAdmin, AdminRole.SuperAdmin);
            AddAdminRolePolicy(options, AdminAuthConstants.PolicyOperations, AdminRole.Operations);
            AddAdminRolePolicy(options, AdminAuthConstants.PolicySupport, AdminRole.Support);
            AddAdminRolePolicy(options, AdminAuthConstants.PolicyTechnical, AdminRole.Technical);
            AddAdminRolePolicy(options, AdminAuthConstants.PolicyAnalyst, AdminRole.Analyst);
            options.AddPolicy(AdminAuthConstants.PolicyUsersManage, policy =>
            {
                policy.AddAuthenticationSchemes(AdminAuthConstants.Scheme);
                policy.RequireAuthenticatedUser();
                policy.RequireRole(AdminRole.SuperAdmin.ToString(), AdminRole.Support.ToString());
            });
            options.AddPolicy(AdminAuthConstants.PolicyMediaManage, policy =>
            {
                policy.AddAuthenticationSchemes(AdminAuthConstants.Scheme);
                policy.RequireAuthenticatedUser();
                policy.RequireRole(
                    AdminRole.SuperAdmin.ToString(),
                    AdminRole.Support.ToString(),
                    AdminRole.Operations.ToString());
            });
            options.AddPolicy(AdminAuthConstants.PolicyPlatformsManage, policy =>
            {
                policy.AddAuthenticationSchemes(AdminAuthConstants.Scheme);
                policy.RequireAuthenticatedUser();
                policy.RequireRole(AdminRole.SuperAdmin.ToString(), AdminRole.Operations.ToString());
            });
            options.AddPolicy(AdminAuthConstants.PolicySettingsManage, policy =>
            {
                policy.AddAuthenticationSchemes(AdminAuthConstants.Scheme);
                policy.RequireAuthenticatedUser();
                policy.RequireRole(AdminRole.SuperAdmin.ToString());
            });
        });

        return services;
    }

    private static void AddAdminRolePolicy(
        AuthorizationOptions options,
        string policyName,
        AdminRole role)
    {
        options.AddPolicy(policyName, policy =>
        {
            policy.AddAuthenticationSchemes(AdminAuthConstants.Scheme);
            policy.RequireAuthenticatedUser();
            policy.RequireClaim(
                AdminJwtTokenService.TokenTypeClaim,
                AdminJwtTokenService.TokenTypeValue);
            policy.RequireRole(role.ToString());
        });
    }

    private static void ConfigureBearer(
        JwtBearerOptions bearerOptions,
        string issuer,
        string audience,
        string signingKey)
    {
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
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = CreateSigningKey(signingKey),
            NameClaimType = "sub",
            RoleClaimType = "role",
        };

        bearerOptions.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
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
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json";
                var payload = JsonSerializer.Serialize(new
                {
                    type = "https://httpstatuses.com/403",
                    title = "Forbidden",
                    status = 403,
                    detail = "You do not have permission to access this resource.",
                });
                await context.Response.WriteAsync(payload);
            },
        };
    }

    private static SymmetricSecurityKey CreateSigningKey(string signingKey)
    {
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException(
                "JWT signing key is not configured. Set Jwt:SigningKey / AdminJwt:SigningKey via configuration or environment variables.");
        }

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
    }
}
