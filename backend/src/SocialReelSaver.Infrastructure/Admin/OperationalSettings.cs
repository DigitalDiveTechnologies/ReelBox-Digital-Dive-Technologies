using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SocialReelSaver.Application.Abstractions.Admin;
using SocialReelSaver.Domain.Entities;
using SocialReelSaver.Infrastructure.Persistence;

namespace SocialReelSaver.Infrastructure.Admin;

public sealed class OperationalSettings(IServiceScopeFactory scopeFactory) : IOperationalSettings
{
    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private volatile bool _loaded;

    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (_loaded) return;
        await RefreshAsync(cancellationToken);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var rows = await db.SystemSettings.AsNoTracking().ToListAsync(cancellationToken);
            _cache.Clear();
            foreach (var row in rows)
                _cache[row.Key] = row.Value;
            _loaded = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public bool GetBool(string key, bool fallback) =>
        TryGet(key, out var v) && bool.TryParse(v, out var b) ? b : fallback;

    public int GetInt(string key, int fallback) =>
        TryGet(key, out var v) && int.TryParse(v, out var i) ? i : fallback;

    public string GetString(string key, string fallback) =>
        TryGet(key, out var v) && v is not null ? v : fallback;

    public bool TryGet(string key, out string? value) => _cache.TryGetValue(key, out value);

    public async Task SetAsync(string key, string value, string category, Guid? adminId, CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = await db.SystemSettings.FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
        if (existing is null)
        {
            db.SystemSettings.Add(new SystemSetting
            {
                Key = key,
                Value = value,
                Category = category,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedByAdminId = adminId,
            });
        }
        else
        {
            existing.Value = value;
            existing.Category = category;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            existing.UpdatedByAdminId = adminId;
        }

        await db.SaveChangesAsync(cancellationToken);
        _cache[key] = value;
        _loaded = true;
    }

    public IReadOnlyDictionary<string, string> Snapshot() =>
        new Dictionary<string, string>(_cache, StringComparer.OrdinalIgnoreCase);
}
