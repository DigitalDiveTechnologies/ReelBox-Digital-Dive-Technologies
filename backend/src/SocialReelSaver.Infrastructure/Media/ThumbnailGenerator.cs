using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialReelSaver.Application.Abstractions.Downloading;
using SocialReelSaver.Application.Abstractions.Media;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Infrastructure.Media;

/// <summary>
/// Extracts a JPEG thumbnail frame via FFmpeg (SRS FR-010).
/// Skips gracefully when FFmpeg is unavailable so downloads still complete.
/// </summary>
public sealed class ThumbnailGenerator : IThumbnailService
{
    private readonly ITemporaryFileManager _tempFiles;
    private readonly FfmpegOptions _options;
    private readonly ILogger<ThumbnailGenerator> _logger;

    public ThumbnailGenerator(
        ITemporaryFileManager tempFiles,
        IOptions<FfmpegOptions> options,
        ILogger<ThumbnailGenerator> logger)
    {
        _tempFiles = tempFiles;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ThumbnailResult> GenerateAsync(
        string mediaLocalPath,
        Guid mediaId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(mediaLocalPath) || !File.Exists(mediaLocalPath))
        {
            return ThumbnailResult.Failed("THUMBNAIL_SOURCE_MISSING", "Media file is missing for thumbnail extraction.");
        }

        var executable = string.IsNullOrWhiteSpace(_options.ExecutablePath)
            ? "ffmpeg"
            : _options.ExecutablePath.Trim();

        var outputPath = _tempFiles.CreateTempFilePath(mediaId, ".jpg");
        var seek = string.IsNullOrWhiteSpace(_options.SeekPosition)
            ? "00:00:01"
            : _options.SeekPosition.Trim();
        var timeoutSeconds = Math.Clamp(_options.TimeoutSeconds, 5, 120);

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                ArgumentList =
                {
                    "-hide_banner",
                    "-loglevel",
                    "error",
                    "-y",
                    "-ss",
                    seek,
                    "-i",
                    mediaLocalPath,
                    "-frames:v",
                    "1",
                    "-q:v",
                    "2",
                    outputPath,
                },
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = new Process { StartInfo = startInfo };
            try
            {
                if (!process.Start())
                {
                    return ThumbnailResult.Skipped($"Unable to start FFmpeg ('{executable}').");
                }
            }
            catch (Exception ex) when (ex is FileNotFoundException or System.ComponentModel.Win32Exception)
            {
                _logger.LogInformation(
                    "FFmpeg not available for media {MediaId}; skipping thumbnail ({Message})",
                    mediaId,
                    ex.Message);
                return ThumbnailResult.Skipped($"FFmpeg not available ('{executable}').");
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                TryKill(process);
                await _tempFiles.CleanupAsync(outputPath, CancellationToken.None);
                return ThumbnailResult.Failed("THUMBNAIL_TIMEOUT", "FFmpeg thumbnail extraction timed out.");
            }

            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
            if (process.ExitCode != 0 || !File.Exists(outputPath) || new FileInfo(outputPath).Length <= 0)
            {
                await _tempFiles.CleanupAsync(outputPath, CancellationToken.None);
                var detail = string.IsNullOrWhiteSpace(stderr) ? $"exit code {process.ExitCode}" : stderr.Trim();
                _logger.LogWarning(
                    "FFmpeg thumbnail failed for media {MediaId}: {Detail}",
                    mediaId,
                    detail);
                return ThumbnailResult.Failed("THUMBNAIL_FAILED", detail);
            }

            return ThumbnailResult.Ok(outputPath, "image/jpeg");
        }
        catch (OperationCanceledException)
        {
            await _tempFiles.CleanupAsync(outputPath, CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            await _tempFiles.CleanupAsync(outputPath, CancellationToken.None);
            _logger.LogWarning(ex, "Unexpected thumbnail failure for media {MediaId}", mediaId);
            return ThumbnailResult.Failed("THUMBNAIL_FAILED", ex.Message);
        }
    }

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
}
