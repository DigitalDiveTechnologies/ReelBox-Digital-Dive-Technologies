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
/// Resolves public Instagram/Facebook media via yt-dlp (new client scope: public share → download).
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
        var timeoutSeconds = Math.Clamp(_options.YtDlpTimeoutSeconds, 30, 300);

        try
        {
            // Metadata first (title / availability) without downloading.
            var probe = await RunYtDlpAsync(
                executable,
                [
                    "--no-playlist",
                    "--skip-download",
                    "--dump-single-json",
                    "--no-warnings",
                    originalUrl,
                ],
                timeoutSeconds,
                cancellationToken);

            if (!probe.Success)
            {
                return MapYtDlpFailure(probe.StdErr, probe.ExitCode);
            }

            string? title = null;
            try
            {
                using var doc = JsonDocument.Parse(probe.StdOut);
                title = doc.RootElement.TryGetProperty("title", out var titleProp)
                    ? titleProp.GetString()
                    : null;
                if (string.IsNullOrWhiteSpace(title) &&
                    doc.RootElement.TryGetProperty("uploader", out var uploaderProp))
                {
                    title = uploaderProp.GetString();
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "yt-dlp JSON probe parse failed for media {MediaId}", mediaId);
            }

            // Download best mp4-compatible stream into worker temp storage.
            var download = await RunYtDlpAsync(
                executable,
                [
                    "--no-playlist",
                    "--no-warnings",
                    "-f",
                    "bv*[ext=mp4]+ba[ext=m4a]/b[ext=mp4]/best",
                    "--merge-output-format",
                    "mp4",
                    "-o",
                    outputPath,
                    originalUrl,
                ],
                timeoutSeconds,
                cancellationToken);

            if (!download.Success)
            {
                await _tempFiles.CleanupAsync(outputPath, CancellationToken.None);
                return MapYtDlpFailure(download.StdErr, download.ExitCode) with { Title = title };
            }

            if (!File.Exists(outputPath) || new FileInfo(outputPath).Length <= 0)
            {
                await _tempFiles.CleanupAsync(outputPath, CancellationToken.None);
                return ProviderResult.Failed(
                    ProviderErrorCode.InvalidProviderResponse,
                    "yt-dlp finished without producing a media file.") with
                {
                    Title = title,
                };
            }

            return ProviderResult.Ok(
                originalUrl,
                title: title,
                mimeType: "video/mp4",
                extension: ".mp4",
                localFilePath: outputPath);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await _tempFiles.CleanupAsync(outputPath, CancellationToken.None);
            throw;
        }
        catch (Exception ex) when (ex is FileNotFoundException or System.ComponentModel.Win32Exception)
        {
            await _tempFiles.CleanupAsync(outputPath, CancellationToken.None);
            _logger.LogError(ex, "yt-dlp executable not available ('{Executable}')", executable);
            return ProviderResult.Failed(
                ProviderErrorCode.ConfigurationError,
                $"yt-dlp is not available ('{executable}'). Install it on the worker host.");
        }
        catch (Exception ex)
        {
            await _tempFiles.CleanupAsync(outputPath, CancellationToken.None);
            _logger.LogError(ex, "Unexpected yt-dlp failure for media {MediaId}", mediaId);
            return ProviderResult.Failed(
                ProviderErrorCode.TemporaryFailure,
                "Unexpected failure while resolving media with yt-dlp.");
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
            return Regex.IsMatch(path, @"^/(reel|p|tv)/[^/]+$", RegexOptions.IgnoreCase);
        }

        if (Regex.IsMatch(path, @"^/(reel|reels|videos|watch|share/v)/", RegexOptions.IgnoreCase))
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
