namespace SocialReelSaver.Shared.Configuration;

/// <summary>
/// FFmpeg settings for thumbnail extraction (SRS FR-010).
/// </summary>
public sealed class FfmpegOptions
{
    public const string SectionName = "Ffmpeg";

    /// <summary>
    /// Executable name or absolute path. Defaults to <c>ffmpeg</c> on PATH.
    /// </summary>
    public string ExecutablePath { get; set; } = "ffmpeg";

    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Seek offset used when extracting a still frame (hh:mm:ss).
    /// </summary>
    public string SeekPosition { get; set; } = "00:00:01";
}
