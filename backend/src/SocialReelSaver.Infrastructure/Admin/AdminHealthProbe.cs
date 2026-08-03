using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SocialReelSaver.Application.Abstractions.Admin;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Admin.DTOs;
using SocialReelSaver.Domain.Enums;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Infrastructure.Admin;

public sealed class AdminHealthProbe(
    HealthCheckService healthChecks,
    IMediaRepository media,
    IOptions<ProvidersOptions> providers,
    IOptions<RapidApiOptions> rapidApi,
    IHttpClientFactory httpClientFactory,
    IServiceProvider services) : IAdminHealthProbe
{
    public async Task<SystemHealthOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var report = await healthChecks.CheckHealthAsync(cancellationToken);
        var components = new List<HealthComponentStatus>
        {
            new("api", "Healthy"),
            MapCheck(report, "postgresql", "database"),
            MapCheck(report, "object-storage", "storage"),
        };

        var activeDownloads = (await media.StatusCountsAsync(cancellationToken))
            .GetValueOrDefault(MediaStatus.Downloading);
        var workerRegistered = services.GetServices<IHostedService>()
            .Any(x => x.GetType().Name.Contains("MediaDownloadWorker", StringComparison.Ordinal));
        components.Add(new("backgroundWorker",
            workerRegistered ? "Healthy" : "Degraded",
            $"activeDownloading={activeDownloads}"));
        components.Add(await ProbeProviderAsync("rapidApi", cancellationToken));

        var overall = components.Any(c => c.Status is "Unhealthy") ? "Unhealthy"
            : components.Any(c => c.Status is "Degraded") ? "Degraded"
            : "Healthy";
        return new(overall, components);
    }

    public async Task<HealthComponentStatus> ProbeProviderAsync(string name, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(providers.Value.Resolver, "RapidApi", StringComparison.OrdinalIgnoreCase))
            return new(name, "Skipped", $"Resolver is '{providers.Value.Resolver}'.");

        if (string.IsNullOrWhiteSpace(rapidApi.Value.ApiKey))
            return new(name, "Degraded", "RapidAPI key is not configured.");

        try
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            using var req = new HttpRequestMessage(HttpMethod.Get, rapidApi.Value.BaseUrl.TrimEnd('/') + "/");
            req.Headers.TryAddWithoutValidation("x-rapidapi-host", rapidApi.Value.Host);
            req.Headers.TryAddWithoutValidation("x-rapidapi-key", rapidApi.Value.ApiKey);
            using var resp = await client.SendAsync(req, cancellationToken);
            return new(name, (int)resp.StatusCode < 500 ? "Healthy" : "Degraded", $"HTTP {(int)resp.StatusCode}");
        }
        catch (Exception ex)
        {
            return new(name, "Degraded", ex.GetType().Name);
        }
    }

    private static HealthComponentStatus MapCheck(HealthReport report, string checkName, string displayName)
    {
        if (!report.Entries.TryGetValue(checkName, out var entry))
            return new(displayName, "Unknown", "Check not registered");

        var status = entry.Status switch
        {
            HealthStatus.Healthy => "Healthy",
            HealthStatus.Degraded => "Degraded",
            _ => "Unhealthy",
        };
        return new(displayName, status, entry.Description);
    }
}
