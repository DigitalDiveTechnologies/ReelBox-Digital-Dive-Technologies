using Microsoft.Extensions.Diagnostics.HealthChecks;
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
    IOptions<ProvidersOptions> providers) : IAdminHealthProbe
{
    public async Task<SystemHealthOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var report = await healthChecks.CheckHealthAsync(cancellationToken);
        var components = new List<HealthComponentStatus>
        {
            new("api", "Healthy"),
            MapCheck(report, "postgresql", "database"),
            MapCheck(report, "object-storage", "storage"),
            MapCheck(report, "redis", "redis"),
        };

        var activeDownloads = (await media.StatusCountsAsync(cancellationToken))
            .GetValueOrDefault(MediaStatus.Downloading);
        var redisOk = components.Any(c => c.Name == "redis" && c.Status is "Healthy");
        components.Add(new(
            "backgroundWorker",
            redisOk ? "Healthy" : "Degraded",
            $"hosted=external;activeDownloading={activeDownloads}"));
        components.Add(await ProbeProviderAsync("ytDlp", cancellationToken));
        // Contract stability: keep legacy component name; RapidAPI removed.
        components.Add(new("rapidApi", "Skipped", "RapidAPI removed; resolver is yt-dlp."));

        var overall = components.Any(c => c.Status is "Unhealthy") ? "Unhealthy"
            : components.Any(c => c.Status is "Degraded") ? "Degraded"
            : "Healthy";
        return new(overall, components);
    }

    public Task<HealthComponentStatus> ProbeProviderAsync(string name, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.Equals(name, "rapidApi", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new HealthComponentStatus(
                name, "Skipped", "RapidAPI removed; resolver is yt-dlp."));
        }

        var executable = string.IsNullOrWhiteSpace(providers.Value.YtDlpExecutablePath)
            ? "yt-dlp"
            : providers.Value.YtDlpExecutablePath.Trim();

        try
        {
            var start = new System.Diagnostics.ProcessStartInfo
            {
                FileName = executable,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var process = System.Diagnostics.Process.Start(start);
            if (process is null)
            {
                return Task.FromResult(new HealthComponentStatus(name, "Degraded", "Unable to start yt-dlp."));
            }

            if (!process.WaitForExit(5000))
            {
                try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
                return Task.FromResult(new HealthComponentStatus(name, "Degraded", "yt-dlp version check timed out."));
            }

            var version = process.StandardOutput.ReadToEnd().Trim();
            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(version))
            {
                return Task.FromResult(new HealthComponentStatus(
                    name, "Degraded", $"yt-dlp exit {process.ExitCode}"));
            }

            return Task.FromResult(new HealthComponentStatus(
                name, "Healthy", version.Length > 80 ? version[..80] : version));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new HealthComponentStatus(name, "Degraded", ex.GetType().Name));
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
