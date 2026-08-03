using System.Text;
using Microsoft.EntityFrameworkCore;
using SocialReelSaver.Application.Abstractions.Admin;
using SocialReelSaver.Application.Admin.DTOs;
using SocialReelSaver.Application.Common.Exceptions;
using SocialReelSaver.Domain.Enums;
using SocialReelSaver.Infrastructure.Persistence;

namespace SocialReelSaver.Infrastructure.Admin;

public sealed class AdminMetricsReader(AppDbContext db) : IAdminMetricsReader
{
    /// <summary>
    /// Npgsql timestamptz only accepts DateTimeOffset with Offset=0 (UTC).
    /// <see cref="DateTimeOffset.Date"/> returns Unspecified DateTime and can bind as local offset (+05:00).
    /// </summary>
    private static DateTimeOffset UtcStartOfDay(DateTimeOffset value) =>
        new(DateTime.SpecifyKind(value.UtcDateTime.Date, DateTimeKind.Utc));

    public async Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var today = UtcStartOfDay(DateTimeOffset.UtcNow);
        var totalMedia = await db.MediaItems.CountAsync(cancellationToken);
        var completed = await db.MediaItems.CountAsync(x => x.Status == MediaStatus.Completed, cancellationToken);
        var failed = await db.MediaItems.CountAsync(x => x.Status == MediaStatus.Failed, cancellationToken);
        return new(
            await db.Users.CountAsync(cancellationToken),
            await db.Users.CountAsync(x => x.IsActive, cancellationToken),
            await db.Users.CountAsync(x => !x.IsActive, cancellationToken),
            totalMedia, completed, failed,
            await db.MediaItems.CountAsync(x => x.DownloadedAt != null && x.DownloadedAt >= today, cancellationToken),
            totalMedia == 0 ? 0 : Math.Round(completed * 100m / totalMedia, 2),
            await db.AdminUsers.CountAsync(x => x.IsActive, cancellationToken));
    }

    public async Task<DashboardTrendsResponse> GetTrendsAsync(int days, CancellationToken cancellationToken = default)
    {
        var start = UtcStartOfDay(DateTimeOffset.UtcNow).AddDays(1 - days);
        // Pull rows then group in-memory — avoids Npgsql translation issues with DateTimeOffset.Date.
        var rows = await db.MediaItems.AsNoTracking()
            .Where(x => x.CreatedAt >= start)
            .Select(x => new { x.CreatedAt, x.Status })
            .ToListAsync(cancellationToken);
        var stats = rows
            .GroupBy(x => x.CreatedAt.UtcDateTime.Date)
            .Select(x => new
            {
                Date = x.Key,
                Downloads = x.Count(y => y.Status == MediaStatus.Completed),
                Failures = x.Count(y => y.Status == MediaStatus.Failed),
            })
            .ToList();
        var values = stats.ToDictionary(x => x.Date, x => x);
        var items = Enumerable.Range(0, days).Select(i =>
        {
            var date = start.UtcDateTime.Date.AddDays(i);
            return values.TryGetValue(date, out var x)
                ? new DashboardTrendPoint(date.ToString("yyyy-MM-dd"), x.Downloads, x.Failures)
                : new DashboardTrendPoint(date.ToString("yyyy-MM-dd"), 0, 0);
        }).ToList();
        return new DashboardTrendsResponse(items);
    }

    public async Task<DashboardActivityResponse> GetActivityAsync(int limit, CancellationToken cancellationToken = default)
    {
        var items = await db.AuditLogs.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(limit)
            .Select(x => new DashboardActivityItem(x.Id, x.Action, $"{x.AdminEmail} {x.Action}", x.CreatedAt))
            .ToListAsync(cancellationToken);
        return new DashboardActivityResponse(items);
    }

    public async Task<DownloadsTrendsResponse> GetDownloadsTrendsAsync(int days, CancellationToken cancellationToken = default)
    {
        var trends = await GetTrendsAsync(days, cancellationToken);
        return new DownloadsTrendsResponse(trends.Items);
    }

    public async Task<UserActivityResponse> GetUserActivityAsync(int days, CancellationToken cancellationToken = default)
    {
        var start = UtcStartOfDay(DateTimeOffset.UtcNow).AddDays(1 - days);
        var userRows = await db.Users.AsNoTracking()
            .Where(x => x.CreatedAt >= start)
            .Select(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var downloadRows = await db.MediaItems.AsNoTracking()
            .Where(x => x.CreatedAt >= start)
            .Select(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var userMap = userRows.GroupBy(x => x.UtcDateTime.Date).ToDictionary(g => g.Key, g => g.Count());
        var dlMap = downloadRows.GroupBy(x => x.UtcDateTime.Date).ToDictionary(g => g.Key, g => g.Count());
        var items = Enumerable.Range(0, days).Select(i =>
        {
            var date = start.UtcDateTime.Date.AddDays(i);
            return new UserActivityPoint(
                date.ToString("yyyy-MM-dd"),
                userMap.GetValueOrDefault(date),
                dlMap.GetValueOrDefault(date));
        }).ToList();
        return new(items);
    }

    public async Task<PlatformStatsResponse> GetPlatformStatsAsync(CancellationToken cancellationToken = default)
    {
        var rows = await db.MediaItems.AsNoTracking()
            .GroupBy(x => x.Platform)
            .Select(g => new
            {
                Platform = g.Key,
                Total = g.Count(),
                Completed = g.Count(x => x.Status == MediaStatus.Completed),
                Failed = g.Count(x => x.Status == MediaStatus.Failed),
            })
            .ToListAsync(cancellationToken);
        return new(rows.Select(x => new PlatformStatItem(
            x.Platform.ToString(),
            x.Total,
            x.Completed,
            x.Failed,
            x.Total == 0 ? 0 : Math.Round(x.Completed * 100m / x.Total, 2))).ToList());
    }

    public async Task<ProviderPerformanceResponse> GetProviderPerformanceAsync(CancellationToken cancellationToken = default)
    {
        // Platform aggregates as proxy for provider performance (one provider per platform today).
        var stats = await GetPlatformStatsAsync(cancellationToken);
        return new(stats.Items.Select(x => new ProviderPerformanceItem(x.Platform, x.Completed, x.Failed, x.SuccessRate)).ToList());
    }

    public async Task<string> ExportCsvAsync(string type, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        switch (type.Trim().ToLowerInvariant())
        {
            case "downloads":
            {
                sb.AppendLine("id,userId,platform,status,createdAt,fileSizeBytes");
                var rows = await db.MediaItems.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(5000)
                    .Select(x => new { x.Id, x.UserId, x.Platform, x.Status, x.CreatedAt, x.FileSizeBytes })
                    .ToListAsync(cancellationToken);
                foreach (var x in rows)
                    sb.AppendLine($"{x.Id},{x.UserId},{x.Platform},{x.Status},{x.CreatedAt:O},{x.FileSizeBytes}");
                break;
            }
            case "users":
            {
                sb.AppendLine("id,email,isActive,createdAt");
                var rows = await db.Users.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(5000)
                    .Select(x => new { x.Id, x.Email, x.IsActive, x.CreatedAt })
                    .ToListAsync(cancellationToken);
                foreach (var x in rows)
                    sb.AppendLine($"{x.Id},{Escape(x.Email)},{x.IsActive},{x.CreatedAt:O}");
                break;
            }
            case "platforms":
            {
                sb.AppendLine("platform,total,completed,failed,successRate");
                var stats = await GetPlatformStatsAsync(cancellationToken);
                foreach (var x in stats.Items)
                    sb.AppendLine($"{x.Platform},{x.Total},{x.Completed},{x.Failed},{x.SuccessRate}");
                break;
            }
            default:
                throw new BadRequestException("Export type must be downloads, users, or platforms.");
        }

        return sb.ToString();
    }

    private static string Escape(string value) =>
        value.Contains(',') || value.Contains('"') ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
}
