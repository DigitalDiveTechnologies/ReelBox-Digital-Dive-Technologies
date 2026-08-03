namespace SocialReelSaver.Application.Abstractions.Admin;

public interface IAdminStorageScanner
{
    bool SupportsOrphanScan { get; }

    string ProviderName { get; }

    Task<AdminOrphanScanResult> ScanOrphansAsync(CancellationToken cancellationToken = default);

    Task<AdminStorageCleanupResult> CleanupOrphansAsync(
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken = default);
}

public sealed record AdminOrphanScanResult(
    bool Supported,
    string? Message,
    IReadOnlyList<string> OrphanKeys,
    int FileCount,
    int DbKeyCount);

public sealed record AdminStorageCleanupResult(
    bool Supported,
    string? Message,
    int DeletedCount,
    IReadOnlyList<string> DeletedKeys);
