using SocialReelSaver.Application.Abstractions.Admin;
using SocialReelSaver.Application.Admin.DTOs;
using SocialReelSaver.Application.Common.Exceptions;

namespace SocialReelSaver.Application.Admin.UseCases;

public sealed class GetSettingsAdminUseCase(IOperationalSettings settings)
{
    private static readonly Dictionary<string, string> Defaults = new(StringComparer.OrdinalIgnoreCase)
    {
        [OperationalSettingKeys.PlatformInstagramEnabled] = "true",
        [OperationalSettingKeys.PlatformFacebookEnabled] = "true",
        [OperationalSettingKeys.PlatformInstagramDailyLimit] = "0",
        [OperationalSettingKeys.PlatformFacebookDailyLimit] = "0",
        [OperationalSettingKeys.PlatformMaintenanceMode] = "false",
        [OperationalSettingKeys.ProviderTimeoutSeconds] = "30",
        [OperationalSettingKeys.ProviderPriorityInstagram] = "1",
        [OperationalSettingKeys.ProviderPriorityFacebook] = "2",
        [OperationalSettingKeys.ProviderInstagramEnabled] = "true",
        [OperationalSettingKeys.ProviderFacebookEnabled] = "true",
        [OperationalSettingKeys.SettingsMaintenanceMode] = "false",
        ["feature.downloadRetry.enabled"] = "true",
        ["feature.playback.enabled"] = "true",
        ["feature.thumbnails.enabled"] = "true",
        ["security.requireActiveUser"] = "true",
        ["security.maxFailedLogins"] = "10",
        ["ops.orphanScan.enabled"] = "true",
        ["ops.cleanup.enabled"] = "true",
        ["app.displayName"] = "Social Reel Saver",
        ["app.supportEmail"] = "support@example.com",
    };

    public async Task<SettingsGroupedResponse> HandleAsync(CancellationToken cancellationToken = default)
    {
        await settings.EnsureLoadedAsync(cancellationToken);
        var snapshot = settings.Snapshot();
        var items = OperationalSettingKeys.Allowlist.Select(key =>
        {
            var value = snapshot.TryGetValue(key, out var v) ? v : Defaults.GetValueOrDefault(key, string.Empty);
            return new SettingItemResponse(key, value, OperationalSettingKeys.CategoryFor(key));
        }).ToList();

        var groups = items
            .GroupBy(x => x.Category)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<SettingItemResponse>)g.OrderBy(x => x.Key).ToList());
        return new(groups);
    }
}

public sealed class UpsertSettingsAdminUseCase(IOperationalSettings settings, IAuditLogWriter audit)
{
    public async Task<SettingsGroupedResponse> HandleAsync(
        IReadOnlyDictionary<string, string> updates,
        Guid adminId, string adminEmail, string? ip, string? correlationId,
        CancellationToken cancellationToken = default)
    {
        await settings.EnsureLoadedAsync(cancellationToken);
        foreach (var (key, value) in updates)
        {
            if (OperationalSettingKeys.IsSecretKey(key))
                throw new BadRequestException($"Key '{key}' looks like a secret and is rejected.");
            if (!OperationalSettingKeys.Allowlist.Contains(key))
                throw new BadRequestException($"Key '{key}' is not allowlisted.");

            await settings.SetAsync(key, value, OperationalSettingKeys.CategoryFor(key), adminId, cancellationToken);
        }

        await audit.WriteAsync(adminId, adminEmail, "settings.updated", "SystemSetting", null,
            null, updates, ip, correlationId, cancellationToken);

        return await new GetSettingsAdminUseCase(settings).HandleAsync(cancellationToken);
    }
}
