using Microsoft.Extensions.Options;
using SocialReelSaver.Application.Abstractions.Admin;
using SocialReelSaver.Application.Admin.DTOs;
using SocialReelSaver.Application.Common.Exceptions;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Application.Admin.UseCases;

public sealed class ListProvidersAdminUseCase(
    IOperationalSettings settings,
    IOptions<ProvidersOptions> providers,
    IOptions<RapidApiOptions> rapidApi)
{
    public async Task<IReadOnlyList<ProviderAdminItem>> HandleAsync(CancellationToken cancellationToken = default)
    {
        await settings.EnsureLoadedAsync(cancellationToken);
        var opts = providers.Value;
        var timeout = settings.GetInt(OperationalSettingKeys.ProviderTimeoutSeconds, opts.TimeoutSeconds);
        var hasToken = !string.IsNullOrWhiteSpace(opts.AccessToken);
        var hasKey = !string.IsNullOrWhiteSpace(rapidApi.Value.ApiKey);
        var health = string.Equals(opts.Resolver, "RapidApi", StringComparison.OrdinalIgnoreCase) && !hasKey
            ? "Degraded"
            : "Unknown";

        return
        [
            Map("InstagramProvider", "instagram", OperationalSettingKeys.ProviderInstagramEnabled,
                OperationalSettingKeys.ProviderPriorityInstagram, opts.Instagram.Enabled, opts.Instagram.RetryEligible,
                timeout, opts.Resolver, health, hasToken, hasKey),
            Map("FacebookProvider", "facebook", OperationalSettingKeys.ProviderFacebookEnabled,
                OperationalSettingKeys.ProviderPriorityFacebook, opts.Facebook.Enabled, opts.Facebook.RetryEligible,
                timeout, opts.Resolver, health, hasToken, hasKey),
        ];
    }

    private ProviderAdminItem Map(
        string name, string platform, string enabledKey, string priorityKey,
        bool configEnabled, bool retryEligible, int timeout, string resolver, string health,
        bool hasToken, bool hasKey)
    {
        var enabled = settings.GetBool(enabledKey, configEnabled);
        var priority = settings.GetInt(priorityKey, platform == "instagram" ? 1 : 2);
        return new(name, platform, enabled, timeout, priority, resolver, health, hasToken, hasKey, retryEligible);
    }
}

public sealed class UpdateProviderAdminUseCase(
    IOperationalSettings settings,
    IOptions<ProvidersOptions> providers,
    IOptions<RapidApiOptions> rapidApi,
    IAuditLogWriter audit)
{
    public async Task<ProviderAdminItem> HandleAsync(
        string name, UpdateProviderRequest request,
        Guid adminId, string adminEmail, string? ip, string? correlationId,
        CancellationToken cancellationToken = default)
    {
        await settings.EnsureLoadedAsync(cancellationToken);
        var n = name.Trim();
        var platform = n.Contains("facebook", StringComparison.OrdinalIgnoreCase) ? "facebook"
            : n.Contains("instagram", StringComparison.OrdinalIgnoreCase) ? "instagram"
            : throw new BadRequestException("Unknown provider name.");

        var enabledKey = platform == "instagram" ? OperationalSettingKeys.ProviderInstagramEnabled : OperationalSettingKeys.ProviderFacebookEnabled;
        var priorityKey = platform == "instagram" ? OperationalSettingKeys.ProviderPriorityInstagram : OperationalSettingKeys.ProviderPriorityFacebook;

        if (request.TimeoutSeconds is not null)
            await settings.SetAsync(OperationalSettingKeys.ProviderTimeoutSeconds, Math.Clamp(request.TimeoutSeconds.Value, 1, 300).ToString(), "providers", adminId, cancellationToken);
        if (request.Priority is not null)
            await settings.SetAsync(priorityKey, request.Priority.Value.ToString(), "providers", adminId, cancellationToken);
        if (request.Enabled is not null)
            await settings.SetAsync(enabledKey, request.Enabled.Value ? "true" : "false", "providers", adminId, cancellationToken);

        await audit.WriteAsync(adminId, adminEmail, "provider.updated", "Provider", n,
            null, new { request.TimeoutSeconds, request.Priority, request.Enabled }, ip, correlationId, cancellationToken);

        var list = await new ListProvidersAdminUseCase(settings, providers, rapidApi).HandleAsync(cancellationToken);
        return list.First(x => x.Platform == platform);
    }
}
