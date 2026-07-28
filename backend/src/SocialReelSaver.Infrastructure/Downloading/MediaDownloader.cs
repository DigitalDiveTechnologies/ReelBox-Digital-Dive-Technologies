using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialReelSaver.Application.Abstractions.Downloading;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Infrastructure.Downloading;

public sealed class TemporaryFileManager : ITemporaryFileManager
{
    private readonly DownloadOptions _options;

    public TemporaryFileManager(IOptions<DownloadOptions> options)
    {
        _options = options.Value;
    }

    public string CreateTempFilePath(Guid mediaId, string? extension = null)
    {
        var root = Path.GetFullPath(_options.TempFolder);
        var mediaDir = Path.Combine(root, mediaId.ToString("N"));
        Directory.CreateDirectory(mediaDir);

        var ext = string.IsNullOrWhiteSpace(extension)
            ? ".bin"
            : extension.StartsWith('.') ? extension : "." + extension;

        return Path.Combine(mediaDir, $"{Guid.NewGuid():N}{ext}");
    }

    public Task CleanupAsync(string? path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    public Task CleanupMediaTempAsync(Guid mediaId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = Path.GetFullPath(_options.TempFolder);
        var mediaDir = Path.Combine(root, mediaId.ToString("N"));
        if (Directory.Exists(mediaDir))
        {
            Directory.Delete(mediaDir, recursive: true);
        }

        return Task.CompletedTask;
    }
}

public sealed class MediaDownloader : IMediaDownloader
{
    private readonly HttpClient _httpClient;
    private readonly ITemporaryFileManager _tempFiles;
    private readonly DownloadOptions _options;
    private readonly ILogger<MediaDownloader> _logger;

    public MediaDownloader(
        HttpClient httpClient,
        ITemporaryFileManager tempFiles,
        IOptions<DownloadOptions> options,
        ILogger<MediaDownloader> logger)
    {
        _httpClient = httpClient;
        _tempFiles = tempFiles;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<MediaDownloadResult> DownloadAsync(
        DownloadContext context,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(context.SourceUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return MediaDownloadResult.Failed("INVALID_MEDIA", "Resolved media source URL is not a valid HTTP(S) URL.");
        }

        var extension = GuessExtension(context.SuggestedFileName, context.SuggestedMimeType, uri);
        var tempPath = _tempFiles.CreateTempFilePath(context.MediaId, extension);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds)));

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            // Facebook CDN thumbnail hosts often require a social Referer.
            if (uri.Host.Contains("fbcdn", StringComparison.OrdinalIgnoreCase) ||
                uri.Host.Contains("fbsbx", StringComparison.OrdinalIgnoreCase) ||
                uri.Host.Contains("facebook.com", StringComparison.OrdinalIgnoreCase))
            {
                request.Headers.TryAddWithoutValidation("Referer", "https://www.facebook.com/");
            }
            else if (uri.Host.Contains("cdninstagram", StringComparison.OrdinalIgnoreCase) ||
                     uri.Host.Contains("instagram.com", StringComparison.OrdinalIgnoreCase))
            {
                request.Headers.TryAddWithoutValidation("Referer", "https://www.instagram.com/");
            }

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                timeoutCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                await _tempFiles.CleanupAsync(tempPath, cancellationToken);
                var code = (int)response.StatusCode is >= 500 or 429
                    ? "PROVIDER_TEMPORARY_FAILURE"
                    : "MEDIA_NOT_FOUND";
                return MediaDownloadResult.Failed(code, $"Download failed with HTTP {(int)response.StatusCode}.");
            }

            var contentType = response.Content.Headers.ContentType?.MediaType
                ?? context.SuggestedMimeType;

            if (response.Content.Headers.ContentLength is long declared &&
                declared > _options.MaxFileSizeBytes)
            {
                await _tempFiles.CleanupAsync(tempPath, cancellationToken);
                return MediaDownloadResult.Failed(
                    "FILE_TOO_LARGE",
                    $"Remote content length {declared} exceeds configured maximum {_options.MaxFileSizeBytes}.");
            }

            await using var remote = await response.Content.ReadAsStreamAsync(timeoutCts.Token);
            await using var local = new FileStream(
                tempPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81_920,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);

            var buffer = new byte[81_920];
            long total = 0;
            while (true)
            {
                var read = await remote.ReadAsync(buffer.AsMemory(0, buffer.Length), timeoutCts.Token);
                if (read == 0)
                {
                    break;
                }

                total += read;
                if (total > _options.MaxFileSizeBytes)
                {
                    await local.DisposeAsync();
                    await _tempFiles.CleanupAsync(tempPath, cancellationToken);
                    return MediaDownloadResult.Failed(
                        "FILE_TOO_LARGE",
                        $"Downloaded size exceeded configured maximum {_options.MaxFileSizeBytes}.");
                }

                await local.WriteAsync(buffer.AsMemory(0, read), timeoutCts.Token);
            }

            await local.FlushAsync(timeoutCts.Token);

            _logger.LogInformation(
                "Downloaded media {MediaId} job {JobId} ({Bytes} bytes) attempt {Attempt}",
                context.MediaId,
                context.JobId,
                total,
                context.Attempt);

            return MediaDownloadResult.Ok(tempPath, contentType, total);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await _tempFiles.CleanupAsync(tempPath, CancellationToken.None);
            return MediaDownloadResult.Failed("DOWNLOAD_TIMEOUT", "Media download timed out.");
        }
        catch (HttpRequestException ex)
        {
            await _tempFiles.CleanupAsync(tempPath, CancellationToken.None);
            _logger.LogWarning(ex, "HTTP download failed for media {MediaId}", context.MediaId);
            return MediaDownloadResult.Failed("PROVIDER_TEMPORARY_FAILURE", "Network error while downloading media.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await _tempFiles.CleanupAsync(tempPath, CancellationToken.None);
            _logger.LogError(ex, "Unexpected download failure for media {MediaId}", context.MediaId);
            return MediaDownloadResult.Failed("UNKNOWN", "Unexpected download failure.");
        }
    }

    private static string GuessExtension(string? fileName, string? mime, Uri uri)
    {
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            var ext = Path.GetExtension(fileName);
            if (!string.IsNullOrWhiteSpace(ext))
            {
                return ext;
            }
        }

        var pathExt = Path.GetExtension(uri.AbsolutePath);
        if (!string.IsNullOrWhiteSpace(pathExt) && pathExt.Length <= 8)
        {
            return pathExt;
        }

        return mime?.ToLowerInvariant() switch
        {
            "video/mp4" => ".mp4",
            "video/quicktime" => ".mov",
            "video/webm" => ".webm",
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".bin",
        };
    }
}

public sealed class DownloadValidator : IDownloadValidator
{
    private readonly DownloadOptions _options;

    public DownloadValidator(IOptions<DownloadOptions> options)
    {
        _options = options.Value;
    }

    public async Task<MediaValidationResult> ValidateAsync(
        string localFilePath,
        string? declaredContentType = null,
        long? suggestedDurationMs = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(localFilePath) || !File.Exists(localFilePath))
        {
            return MediaValidationResult.Failed("INVALID_MEDIA", "Downloaded media file does not exist.");
        }

        var info = new FileInfo(localFilePath);
        if (info.Length <= 0)
        {
            return MediaValidationResult.Failed("INVALID_MEDIA", "Downloaded media file is empty.");
        }

        if (info.Length > _options.MaxFileSizeBytes)
        {
            return MediaValidationResult.Failed(
                "FILE_TOO_LARGE",
                $"File size {info.Length} exceeds configured maximum {_options.MaxFileSizeBytes}.");
        }

        var mime = DetectMimeType(localFilePath, declaredContentType);
        if (!_options.AllowedMimeTypes.Contains(mime, StringComparer.OrdinalIgnoreCase))
        {
            return MediaValidationResult.Failed(
                "UNSUPPORTED_MEDIA_TYPE",
                $"MIME type '{mime}' is not allowed.");
        }

        // Placeholder duration — real probe (FFmpeg/ffprobe) deferred.
        var durationMs = suggestedDurationMs;
        var durationPlaceholder = durationMs is null;

        // Placeholder checksum — compute SHA-256 for integrity bookkeeping; treat as provisional.
        string? checksum = null;
        var checksumPlaceholder = true;
        try
        {
            await using var stream = File.OpenRead(localFilePath);
            var hash = await System.Security.Cryptography.SHA256.HashDataAsync(stream, cancellationToken);
            checksum = Convert.ToHexString(hash).ToLowerInvariant();
            checksumPlaceholder = true; // reserved until content-addressed storage policy is finalized
        }
        catch (Exception)
        {
            checksum = null;
            checksumPlaceholder = true;
        }

        return MediaValidationResult.Ok(
            mime,
            info.Length,
            durationMs,
            checksum,
            durationPlaceholder,
            checksumPlaceholder);
    }

    private static string DetectMimeType(string path, string? declared)
    {
        if (!string.IsNullOrWhiteSpace(declared) &&
            !declared.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            return declared;
        }

        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".webm" => "video/webm",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => declared ?? "application/octet-stream",
        };
    }
}
