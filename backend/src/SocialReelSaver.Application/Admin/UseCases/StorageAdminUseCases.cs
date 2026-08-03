using SocialReelSaver.Application.Abstractions.Admin;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Admin.DTOs;

namespace SocialReelSaver.Application.Admin.UseCases;

public sealed class GetStorageSummaryUseCase(IMediaRepository media, IAdminStorageScanner scanner)
{
    public async Task<StorageSummaryResponse> HandleAsync(CancellationToken cancellationToken = default)
    {
        var counts = await media.StatusCountsAsync(cancellationToken);
        var totalBytes = await media.SumFileSizeBytesAsync(cancellationToken);
        return new(scanner.ProviderName, counts.Values.Sum(), totalBytes, null);
    }
}

public sealed class ScanStorageOrphansUseCase(IAdminStorageScanner scanner)
{
    public async Task<OrphanScanResponse> HandleAsync(CancellationToken cancellationToken = default)
    {
        var result = await scanner.ScanOrphansAsync(cancellationToken);
        return new(result.Supported, result.Message, result.OrphanKeys, result.FileCount, result.DbKeyCount);
    }
}

public sealed class CleanupStorageOrphansUseCase(IAdminStorageScanner scanner, IAuditLogWriter audit)
{
    public async Task<StorageCleanupResponse> HandleAsync(
        IReadOnlyList<string> keys, Guid adminId, string adminEmail, string? ip, string? correlationId,
        CancellationToken cancellationToken = default)
    {
        var result = await scanner.CleanupOrphansAsync(keys, cancellationToken);
        if (result.Supported && result.DeletedCount > 0)
        {
            await audit.WriteAsync(adminId, adminEmail, "storage.orphans.cleaned", "Storage", null,
                null, new { result.DeletedCount, result.DeletedKeys }, ip, correlationId, cancellationToken);
        }

        return new(result.Supported, result.Message, result.DeletedCount, result.DeletedKeys);
    }
}
