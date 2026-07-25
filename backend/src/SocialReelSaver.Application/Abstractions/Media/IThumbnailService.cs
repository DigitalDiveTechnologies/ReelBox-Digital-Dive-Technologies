namespace SocialReelSaver.Application.Abstractions.Media;

public sealed class ThumbnailResult
{
    public bool Success { get; init; }

    public bool IsNotImplemented { get; init; }

    public string? LocalThumbnailPath { get; init; }

    public string? ContentType { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public static ThumbnailResult Skipped(string reason) => new()
    {
        Success = false,
        IsNotImplemented = true,
        ErrorCode = "THUMBNAIL_NOT_IMPLEMENTED",
        ErrorMessage = reason,
    };

    public static ThumbnailResult Ok(string path, string contentType) => new()
    {
        Success = true,
        LocalThumbnailPath = path,
        ContentType = contentType,
    };

    public static ThumbnailResult Failed(string code, string message) => new()
    {
        Success = false,
        ErrorCode = code,
        ErrorMessage = message,
    };
}

public interface IThumbnailService
{
    Task<ThumbnailResult> GenerateAsync(
        string mediaLocalPath,
        Guid mediaId,
        CancellationToken cancellationToken = default);
}
