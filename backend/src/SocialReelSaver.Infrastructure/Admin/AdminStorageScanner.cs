using Microsoft.Extensions.Options;
using SocialReelSaver.Application.Abstractions.Admin;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Infrastructure.Storage;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Infrastructure.Admin;

public sealed class AdminStorageScanner(
    IMediaRepository media,
    IOptions<ObjectStorageOptions> storageOptions) : IAdminStorageScanner
{
    public string ProviderName => storageOptions.Value.Provider;

    public bool SupportsOrphanScan =>
        storageOptions.Value.Provider.Trim().Equals("Local", StringComparison.OrdinalIgnoreCase);

    public async Task<AdminOrphanScanResult> ScanOrphansAsync(CancellationToken cancellationToken = default)
    {
        if (!SupportsOrphanScan)
            return new(false, "Orphan scan is only supported for Local object storage.", [], 0, 0);

        var root = LocalStoragePath.Resolve(storageOptions.Value.LocalRootPath);
        if (!Directory.Exists(root))
            return new(true, "Local root path does not exist.", [], 0, 0);

        var dbKeys = await media.ListStorageKeysAsync(cancellationToken);
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (mediaKey, thumbKey) in dbKeys)
        {
            if (!string.IsNullOrWhiteSpace(mediaKey)) known.Add(Normalize(mediaKey));
            if (!string.IsNullOrWhiteSpace(thumbKey)) known.Add(Normalize(thumbKey));
        }

        var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).ToList();
        var orphans = new List<string>();
        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (!known.Contains(relative) && !known.Contains(Normalize(relative)))
                orphans.Add(relative);
        }

        return new(true, null, orphans, files.Count, known.Count);
    }

    public async Task<AdminStorageCleanupResult> CleanupOrphansAsync(
        IReadOnlyList<string> keys, CancellationToken cancellationToken = default)
    {
        if (!SupportsOrphanScan)
            return new(false, "Cleanup is only supported for Local object storage.", 0, []);

        var scan = await ScanOrphansAsync(cancellationToken);
        var allowed = new HashSet<string>(scan.OrphanKeys, StringComparer.OrdinalIgnoreCase);
        var root = LocalStoragePath.Resolve(storageOptions.Value.LocalRootPath);
        var deleted = new List<string>();

        foreach (var key in keys.Where(k => !string.IsNullOrWhiteSpace(k)))
        {
            var normalized = key.Replace('\\', '/').TrimStart('/');
            if (!allowed.Contains(normalized)) continue;

            var full = Path.GetFullPath(Path.Combine(root, normalized));
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) continue;
            if (!File.Exists(full)) continue;

            File.Delete(full);
            deleted.Add(normalized);
        }

        return new(true, null, deleted.Count, deleted);
    }

    private static string Normalize(string key) => key.Replace('\\', '/').TrimStart('/');
}
