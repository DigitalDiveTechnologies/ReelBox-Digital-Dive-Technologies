using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialReelSaver.Application.Abstractions.Downloading;
using SocialReelSaver.Application.Abstractions.Providers;
using SocialReelSaver.Domain.Enums;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Infrastructure.Providers;

/// <summary>
/// Resolves public Instagram/Facebook media via yt-dlp (public share → download).
/// Private / login-gated content fails with AccessNotPermitted / MediaNotFound.
/// </summary>
public sealed class YtDlpMediaResolver
{
    private readonly ITemporaryFileManager _tempFiles;
    private readonly ProvidersOptions _options;
    private readonly ILogger<YtDlpMediaResolver> _logger;

    public YtDlpMediaResolver(
        ITemporaryFileManager tempFiles,
        IOptions<ProvidersOptions> options,
        ILogger<YtDlpMediaResolver> logger)
    {
        _tempFiles = tempFiles;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ProviderResult> ResolveAsync(
        MediaPlatform platform,
        string originalUrl,
        Guid mediaId,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out var sourceUri) ||
            (sourceUri.Scheme != Uri.UriSchemeHttps && sourceUri.Scheme != Uri.UriSchemeHttp))
        {
            return ProviderResult.Failed(
                ProviderErrorCode.InvalidProviderResponse,
                "Original URL is not a valid absolute HTTP(S) URL.");
        }

        if (!IsSupportedContentUrl(platform, sourceUri))
        {
            return ProviderResult.Failed(
                ProviderErrorCode.MediaNotFound,
                "URL does not match a supported Instagram/Facebook media path.");
        }

        var executable = string.IsNullOrWhiteSpace(_options.YtDlpExecutablePath)
            ? "yt-dlp"
            : _options.YtDlpExecutablePath.Trim();

        var outputPath = _tempFiles.CreateTempFilePath(mediaId, ".mp4");
        var timeoutSeconds = Math.Clamp(
            Math.Max(_options.YtDlpTimeoutSeconds, _options.MaximumExecutionSeconds - 15),
            60,
            360);

        try
        {
            // %(ext)s avoids "no file" when yt-dlp writes a non-mp4 container first.
            var outputTemplate = Path.Combine(
                Path.GetDirectoryName(outputPath)!,
                Path.GetFileNameWithoutExtension(outputPath) + ".%(ext)s");

            var format = platform == MediaPlatform.Facebook
                ? "sd/hd/b[ext=mp4]/best[ext=mp4]/best"
                : "b[ext=mp4]/best[ext=mp4]/bv*[ext=mp4]+ba[ext=m4a]/best";

            _logger.LogInformation(
                "yt-dlp download platform={Platform} media={MediaId} timeout={Timeout}s",
                platform,
                mediaId,
                timeoutSeconds);

            var download = await RunYtDlpAsync(
                executable,
                [
                    "--no-playlist",
                    "--no-warnings",
                    "-f",
                    format,
                    "--merge-output-format",
                    "mp4",
                    "--socket-timeout",
                    "90",
                    "--retries",
                    "3",
                    "--fragment-retries",
                    "3",
                    // Title/description for keyword categorization.
                    "--print",
                    "after_move:%()j",
                    // Local thumbnail (jpg/webp/png); pipeline uploads it when present.
                    "--write-thumbnail",
                    "-o",
                    outputTemplate,
                    originalUrl,
                ],
                timeoutSeconds,
                cancellationToken);

            if (!download.Success)
            {
                await CleanupOutputsAsync(outputPath, cancellationToken);
                _logger.LogWarning(
                    "yt-dlp failed for media {MediaId}: {Stderr}",
                    mediaId,
                    Truncate(download.StdErr, 500));
                return MapYtDlpFailure(download.StdErr, download.ExitCode);
            }

            var produced = FindProducedMediaFile(outputPath);
            if (produced is null)
            {
                await CleanupOutputsAsync(outputPath, cancellationToken);
                _logger.LogWarning(
                    "yt-dlp produced no media file for {MediaId}. stderr={Stderr}",
                    mediaId,
                    Truncate(download.StdErr, 400));
                return ProviderResult.Failed(
                    ProviderErrorCode.InvalidProviderResponse,
                    "yt-dlp finished without producing a media file.");
            }

            if (!string.Equals(produced, outputPath, StringComparison.Ordinal))
            {
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }

                File.Move(produced, outputPath, overwrite: true);
            }

            if (!File.Exists(outputPath) || new FileInfo(outputPath).Length <= 0)
            {
                await CleanupOutputsAsync(outputPath, cancellationToken);
                return ProviderResult.Failed(
                    ProviderErrorCode.InvalidProviderResponse,
                    "yt-dlp finished without producing a media file.");
            }

            var meta = ParseMetadata(download.StdOut);
            var localThumb = FindProducedThumbnail(outputPath);

            return ProviderResult.Ok(
                originalUrl,
                title: meta.Title,
                mimeType: "video/mp4",
                extension: ".mp4",
                localFilePath: outputPath,
                thumbnailSourceUrl: meta.ThumbnailUrl,
                localThumbnailPath: localThumb,
                description: meta.Description,
                uploader: meta.Uploader,
                metadataText: meta.MetadataText);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await CleanupOutputsAsync(outputPath, CancellationToken.None);
            throw;
        }
        catch (Exception ex) when (ex is FileNotFoundException or System.ComponentModel.Win32Exception)
        {
            await CleanupOutputsAsync(outputPath, CancellationToken.None);
            _logger.LogError(ex, "yt-dlp executable not available ('{Executable}')", executable);
            return ProviderResult.Failed(
                ProviderErrorCode.ConfigurationError,
                $"yt-dlp is not available ('{executable}'). Install it on the worker host.");
        }
        catch (Exception ex)
        {
            await CleanupOutputsAsync(outputPath, CancellationToken.None);
            _logger.LogError(ex, "Unexpected yt-dlp failure for media {MediaId}", mediaId);
            return ProviderResult.Failed(
                ProviderErrorCode.TemporaryFailure,
                "Unexpected failure while resolving media with yt-dlp.");
        }
    }

    private sealed record YtDlpParsedMetadata(
        string? Title,
        string? Description,
        string? Uploader,
        string? MetadataText,
        string? ThumbnailUrl);

    private static YtDlpParsedMetadata ParseMetadata(string stdout)
    {
        if (string.IsNullOrWhiteSpace(stdout))
        {
            return new YtDlpParsedMetadata(null, null, null, null, null);
        }

        // after_move print may include progress lines; take the last JSON-looking line.
        string? jsonLine = null;
        using (var reader = new StringReader(stdout))
        {
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("{", StringComparison.Ordinal) &&
                    trimmed.EndsWith("}", StringComparison.Ordinal))
                {
                    jsonLine = trimmed;
                }
            }
        }

        if (jsonLine is null)
        {
            return new YtDlpParsedMetadata(null, null, null, null, null);
        }

        try
        {
            using var doc = JsonDocument.Parse(jsonLine);
            var root = doc.RootElement;

            var title = GetString(root, "title") ?? GetString(root, "fulltitle");
            var description = GetString(root, "description") ?? GetString(root, "summary");
            var uploader = FirstNonEmpty(
                GetString(root, "uploader"),
                GetString(root, "channel"),
                GetString(root, "creator"),
                GetString(root, "uploader_id"),
                GetString(root, "channel_id"),
                GetString(root, "artist"));

            var extras = new List<string>();
            AppendJoined(extras, GetStringArray(root, "tags"));
            AppendJoined(extras, GetStringArray(root, "categories"));
            AppendIfPresent(extras, GetString(root, "track"));
            AppendIfPresent(extras, GetString(root, "album"));
            AppendIfPresent(extras, GetString(root, "genre"));
            AppendIfPresent(extras, GetString(root, "series"));
            AppendIfPresent(extras, GetString(root, "season"));
            AppendIfPresent(extras, GetString(root, "episode"));
            AppendIfPresent(extras, GetString(root, "alt_title"));
            AppendIfPresent(extras, GetString(root, "display_id"));
            AppendIfPresent(extras, GetString(root, "extractor"));
            AppendIfPresent(extras, GetString(root, "extractor_key"));

            // Filename stem from requested downloads / _filename when present.
            AppendIfPresent(extras, GetString(root, "_filename"));
            AppendIfPresent(extras, GetString(root, "filename"));
            if (root.TryGetProperty("requested_downloads", out var downloads) &&
                downloads.ValueKind == JsonValueKind.Array)
            {
                foreach (var d in downloads.EnumerateArray())
                {
                    AppendIfPresent(extras, GetString(d, "filename"));
                    AppendIfPresent(extras, GetString(d, "_filename"));
                }
            }

            var metadataText = extras.Count == 0
                ? null
                : string.Join(' ', extras.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase));

            string? thumb = GetString(root, "thumbnail");
            if (string.IsNullOrWhiteSpace(thumb) &&
                root.TryGetProperty("thumbnails", out var thumbs) &&
                thumbs.ValueKind == JsonValueKind.Array)
            {
                foreach (var t in thumbs.EnumerateArray().Reverse())
                {
                    thumb = GetString(t, "url");
                    if (!string.IsNullOrWhiteSpace(thumb))
                    {
                        break;
                    }
                }
            }

            return new YtDlpParsedMetadata(title, description, uploader, metadataText, thumb);
        }
        catch (JsonException)
        {
            return new YtDlpParsedMetadata(null, null, null, null, null);
        }
    }

    private static string? GetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;

    private static IEnumerable<string> GetStringArray(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var prop))
        {
            yield break;
        }

        if (prop.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in prop.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var s = item.GetString();
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        yield return s!;
                    }
                }
            }
        }
        else if (prop.ValueKind == JsonValueKind.String)
        {
            var s = prop.GetString();
            if (!string.IsNullOrWhiteSpace(s))
            {
                yield return s!;
            }
        }
    }

    private static void AppendJoined(List<string> target, IEnumerable<string> values)
    {
        foreach (var v in values)
        {
            AppendIfPresent(target, v);
        }
    }

    private static void AppendIfPresent(List<string> target, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            target.Add(value.Trim());
        }
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static string? FindProducedMediaFile(string expectedMp4Path)
    {
        if (File.Exists(expectedMp4Path) && new FileInfo(expectedMp4Path).Length > 0)
        {
            return expectedMp4Path;
        }

        var dir = Path.GetDirectoryName(expectedMp4Path);
        var stem = Path.GetFileNameWithoutExtension(expectedMp4Path);
        if (string.IsNullOrWhiteSpace(dir) || string.IsNullOrWhiteSpace(stem))
        {
            return null;
        }

        var mediaExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".mkv", ".webm", ".mov", ".m4a", ".mp3",
        };

        return Directory.EnumerateFiles(dir, stem + ".*")
            .Where(path => !path.EndsWith(".part", StringComparison.OrdinalIgnoreCase))
            .Where(path => mediaExts.Contains(Path.GetExtension(path)))
            .Where(path => new FileInfo(path).Length > 0)
            .OrderByDescending(path => new FileInfo(path).Length)
            .FirstOrDefault();
    }

    private static string? FindProducedThumbnail(string expectedMp4Path)
    {
        var dir = Path.GetDirectoryName(expectedMp4Path);
        var stem = Path.GetFileNameWithoutExtension(expectedMp4Path);
        if (string.IsNullOrWhiteSpace(dir) || string.IsNullOrWhiteSpace(stem))
        {
            return null;
        }

        var thumbExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp",
        };

        return Directory.EnumerateFiles(dir, stem + ".*")
            .Where(path => thumbExts.Contains(Path.GetExtension(path)))
            .Where(path => new FileInfo(path).Length > 0)
            .OrderByDescending(path => path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(path => new FileInfo(path).Length)
            .FirstOrDefault();
    }

    private async Task CleanupOutputsAsync(string expectedMp4Path, CancellationToken cancellationToken)
    {
        await _tempFiles.CleanupAsync(expectedMp4Path, cancellationToken);
        var dir = Path.GetDirectoryName(expectedMp4Path);
        var stem = Path.GetFileNameWithoutExtension(expectedMp4Path);
        if (string.IsNullOrWhiteSpace(dir) || string.IsNullOrWhiteSpace(stem))
        {
            return;
        }

        foreach (var path in Directory.EnumerateFiles(dir, stem + ".*"))
        {
            await _tempFiles.CleanupAsync(path, CancellationToken.None);
        }
    }

    private async Task<YtDlpRunResult> RunYtDlpAsync(
        string executable,
        IReadOnlyList<string> args,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            return new YtDlpRunResult(false, -1, string.Empty, "Unable to start yt-dlp.");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            return new YtDlpRunResult(false, -1, string.Empty, "yt-dlp timed out.");
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        return new YtDlpRunResult(process.ExitCode == 0, process.ExitCode, stdout, stderr);
    }

    private static ProviderResult MapYtDlpFailure(string stderr, int exitCode)
    {
        var detail = string.IsNullOrWhiteSpace(stderr) ? $"yt-dlp exit code {exitCode}" : stderr.Trim();
        var lower = detail.ToLowerInvariant();

        if (lower.Contains("private", StringComparison.Ordinal) ||
            lower.Contains("login required", StringComparison.Ordinal) ||
            lower.Contains("only available for registered users", StringComparison.Ordinal) ||
            lower.Contains("members-only", StringComparison.Ordinal))
        {
            return ProviderResult.Failed(
                ProviderErrorCode.AccessNotPermitted,
                "Media is private or requires login and cannot be downloaded.");
        }

        if (lower.Contains("not found", StringComparison.Ordinal) ||
            lower.Contains("unsupported url", StringComparison.Ordinal) ||
            lower.Contains("no video", StringComparison.Ordinal) ||
            lower.Contains("unable to extract", StringComparison.Ordinal))
        {
            return ProviderResult.Failed(
                ProviderErrorCode.MediaNotFound,
                "Media was not found or is not available for download.");
        }

        if (lower.Contains("timed out", StringComparison.Ordinal) ||
            lower.Contains("timeout", StringComparison.Ordinal) ||
            lower.Contains("429", StringComparison.Ordinal) ||
            lower.Contains("http error 5", StringComparison.Ordinal))
        {
            return ProviderResult.Failed(
                ProviderErrorCode.TemporaryFailure,
                "Media resolver is temporarily unavailable.");
        }

        return ProviderResult.Failed(
            ProviderErrorCode.TemporaryFailure,
            Truncate($"Media resolve failed: {detail}", 400));
    }

    private static bool IsSupportedContentUrl(MediaPlatform platform, Uri uri)
    {
        var path = uri.AbsolutePath.TrimEnd('/');
        if (platform == MediaPlatform.Instagram)
        {
            // reel / reels / p / tv / share links from share sheet & copy link.
            return Regex.IsMatch(
                path,
                @"^/(reel|reels|p|tv|share)/[^/]+",
                RegexOptions.IgnoreCase);
        }

        if (Regex.IsMatch(path, @"^/(reel|reels|videos|watch|share/v|share/r)/", RegexOptions.IgnoreCase))
        {
            return true;
        }

        if (Regex.IsMatch(path, @"^/share/[^/]+$", RegexOptions.IgnoreCase))
        {
            return true;
        }

        if (uri.Host.Contains("fb.watch", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return uri.Query.Contains("v=", StringComparison.OrdinalIgnoreCase);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best-effort cancel.
        }
    }

    private sealed record YtDlpRunResult(bool Success, int ExitCode, string StdOut, string StdErr);
}
