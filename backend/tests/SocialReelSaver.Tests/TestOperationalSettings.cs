using System.Collections.Concurrent;
using SocialReelSaver.Application.Abstractions.Admin;

namespace SocialReelSaver.Tests;

internal sealed class TestOperationalSettings : IOperationalSettings
{
    private readonly ConcurrentDictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);

    public Task EnsureLoadedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public bool GetBool(string key, bool fallback) =>
        _values.TryGetValue(key, out var v) && bool.TryParse(v, out var b) ? b : fallback;

    public int GetInt(string key, int fallback) =>
        _values.TryGetValue(key, out var v) && int.TryParse(v, out var i) ? i : fallback;

    public string GetString(string key, string fallback) =>
        _values.TryGetValue(key, out var v) ? v : fallback;

    public bool TryGet(string key, out string? value) => _values.TryGetValue(key, out value);

    public Task SetAsync(string key, string value, string category, Guid? adminId, CancellationToken cancellationToken = default)
    {
        _values[key] = value;
        return Task.CompletedTask;
    }

    public IReadOnlyDictionary<string, string> Snapshot() =>
        new Dictionary<string, string>(_values, StringComparer.OrdinalIgnoreCase);
}
