using SocialReelSaver.Application.Abstractions.Admin;
using SocialReelSaver.Application.Admin.DTOs;
using SocialReelSaver.Application.Common.Exceptions;
using SocialReelSaver.Shared.Configuration;
using Microsoft.Extensions.Options;

namespace SocialReelSaver.Application.Admin.UseCases;

public sealed class ListPlatformsAdminUseCase(IOperationalSettings settings, IOptions<ProvidersOptions> providers)
{
    public async Task<IReadOnlyList<PlatformAdminItem>> HandleAsync(CancellationToken cancellationToken = default)
    {
        await settings.EnsureLoadedAsync(cancellationToken);
        var maintenance = settings.GetBool(OperationalSettingKeys.PlatformMaintenanceMode, false)
            || settings.GetBool(OperationalSettingKeys.SettingsMaintenanceMode, false);
        return
        [
            Map("instagram", OperationalSettingKeys.PlatformInstagramEnabled,
                OperationalSettingKeys.PlatformInstagramDailyLimit, providers.Value.Instagram.Enabled, maintenance),
            Map("facebook", OperationalSettingKeys.PlatformFacebookEnabled,
                OperationalSettingKeys.PlatformFacebookDailyLimit, providers.Value.Facebook.Enabled, maintenance),
        ];
    }

    private PlatformAdminItem Map(string platform, string enabledKey, string limitKey, bool configEnabled, bool maintenance)
    {
        var enabledFlag = settings.GetBool(enabledKey, configEnabled);
        var limit = settings.GetInt(limitKey, 0);
        var status = maintenance ? "maintenance" : enabledFlag ? "enabled" : "disabled";
        return new(platform, enabledFlag, maintenance, limit, status);
    }
}

public sealed class UpdatePlatformAdminUseCase(IOperationalSettings settings, IOptions<ProvidersOptions> providers, IAuditLogWriter audit)
{
    public async Task<PlatformAdminItem> HandleAsync(
        string platform, UpdatePlatformRequest request,
        Guid adminId, string adminEmail, string? ip, string? correlationId, CancellationToken cancellationToken = default)
    {
        await settings.EnsureLoadedAsync(cancellationToken);
        var p = platform.Trim().ToLowerInvariant();
        if (p is not ("instagram" or "facebook"))
            throw new BadRequestException("Platform must be instagram or facebook.");

        var enabledKey = p == "instagram" ? OperationalSettingKeys.PlatformInstagramEnabled : OperationalSettingKeys.PlatformFacebookEnabled;
        var limitKey = p == "instagram" ? OperationalSettingKeys.PlatformInstagramDailyLimit : OperationalSettingKeys.PlatformFacebookDailyLimit;
        var configEnabled = p == "instagram" ? providers.Value.Instagram.Enabled : providers.Value.Facebook.Enabled;

        var oldEnabled = settings.GetBool(enabledKey, configEnabled);
        var oldLimit = settings.GetInt(limitKey, 0);
        var oldMaint = settings.GetBool(OperationalSettingKeys.PlatformMaintenanceMode, false);

        if (request.Enabled is not null)
            await settings.SetAsync(enabledKey, request.Enabled.Value ? "true" : "false", "platforms", adminId, cancellationToken);
        if (request.DailyLimit is not null)
            await settings.SetAsync(limitKey, Math.Max(0, request.DailyLimit.Value).ToString(), "platforms", adminId, cancellationToken);
        if (request.MaintenanceMode is not null)
            await settings.SetAsync(OperationalSettingKeys.PlatformMaintenanceMode, request.MaintenanceMode.Value ? "true" : "false", "platforms", adminId, cancellationToken);

        await audit.WriteAsync(adminId, adminEmail, "platform.updated", "Platform", p,
            new { enabled = oldEnabled, dailyLimit = oldLimit, maintenanceMode = oldMaint },
            new { enabled = request.Enabled ?? oldEnabled, dailyLimit = request.DailyLimit ?? oldLimit, maintenanceMode = request.MaintenanceMode ?? oldMaint },
            ip, correlationId, cancellationToken);

        var items = await new ListPlatformsAdminUseCase(settings, providers).HandleAsync(cancellationToken);
        return items.First(x => x.Platform == p);
    }
}
