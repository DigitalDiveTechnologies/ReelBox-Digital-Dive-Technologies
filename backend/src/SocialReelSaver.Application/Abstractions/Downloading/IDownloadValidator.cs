namespace SocialReelSaver.Application.Abstractions.Downloading;

public sealed class MediaValidationResult
{
    public bool Success { get; init; }

    public string? MimeType { get; init; }

    public long FileSizeBytes { get; init; }

    public long? DurationMs { get; init; }

    public string? ChecksumSha256 { get; init; }

    public bool DurationIsPlaceholder { get; init; }

    public bool ChecksumIsPlaceholder { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public static MediaValidationResult Ok(
        string mimeType,
        long fileSizeBytes,
        long? durationMs,
        string? checksumSha256,
        bool durationIsPlaceholder,
        bool checksumIsPlaceholder) => new()
    {
        Success = true,
        MimeType = mimeType,
        FileSizeBytes = fileSizeBytes,
        DurationMs = durationMs,
        ChecksumSha256 = checksumSha256,
        DurationIsPlaceholder = durationIsPlaceholder,
        ChecksumIsPlaceholder = checksumIsPlaceholder,
    };

    public static MediaValidationResult Failed(string code, string message) => new()
    {
        Success = false,
        ErrorCode = code,
        ErrorMessage = message,
    };
}

public interface IDownloadValidator
{
    Task<MediaValidationResult> ValidateAsync(
        string localFilePath,
        string? declaredContentType = null,
        long? suggestedDurationMs = null,
        CancellationToken cancellationToken = default);
}
