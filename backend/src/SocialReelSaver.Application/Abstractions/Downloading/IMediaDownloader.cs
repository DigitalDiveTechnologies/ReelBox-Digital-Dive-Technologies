namespace SocialReelSaver.Application.Abstractions.Downloading;

public sealed class DownloadContext
{
    public required Guid MediaId { get; init; }

    public required Guid JobId { get; init; }

    public required string SourceUrl { get; init; }

    public string? SuggestedFileName { get; init; }

    public string? SuggestedMimeType { get; init; }

    public int Attempt { get; init; }
}

public sealed class MediaDownloadResult
{
    public bool Success { get; init; }

    public string? LocalFilePath { get; init; }

    public string? ContentType { get; init; }

    public long BytesDownloaded { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public static MediaDownloadResult Ok(
        string localFilePath,
        string? contentType,
        long bytesDownloaded) => new()
    {
        Success = true,
        LocalFilePath = localFilePath,
        ContentType = contentType,
        BytesDownloaded = bytesDownloaded,
    };

    public static MediaDownloadResult Failed(string code, string message) => new()
    {
        Success = false,
        ErrorCode = code,
        ErrorMessage = message,
    };
}

public interface IMediaDownloader
{
    Task<MediaDownloadResult> DownloadAsync(
        DownloadContext context,
        CancellationToken cancellationToken = default);
}

public interface ITemporaryFileManager
{
    string CreateTempFilePath(Guid mediaId, string? extension = null);

    Task CleanupAsync(string? path, CancellationToken cancellationToken = default);

    Task CleanupMediaTempAsync(Guid mediaId, CancellationToken cancellationToken = default);
}
