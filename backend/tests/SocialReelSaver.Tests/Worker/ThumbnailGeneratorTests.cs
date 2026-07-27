using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SocialReelSaver.Infrastructure.Downloading;
using SocialReelSaver.Infrastructure.Media;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Tests.Worker;

public sealed class ThumbnailGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_WhenFfmpegMissing_SkipsWithoutThrowing()
    {
        var root = Path.Combine(Path.GetTempPath(), "srs-thumb-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var mediaPath = Path.Combine(root, "sample.bin");
        await File.WriteAllBytesAsync(mediaPath, [0x00, 0x01, 0x02, 0x03]);

        try
        {
            var options = Options.Create(new DownloadOptions { TempFolder = root });
            var tempFiles = new TemporaryFileManager(options);
            var generator = new ThumbnailGenerator(
                tempFiles,
                Options.Create(new FfmpegOptions { ExecutablePath = "__srs_ffmpeg_missing__" }),
                NullLogger<ThumbnailGenerator>.Instance);

            var result = await generator.GenerateAsync(mediaPath, Guid.NewGuid());

            Assert.False(result.Success);
            Assert.True(result.IsNotImplemented);
            Assert.Contains("FFmpeg", result.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task GenerateAsync_WhenSourceMissing_Fails()
    {
        var root = Path.Combine(Path.GetTempPath(), "srs-thumb-miss-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var options = Options.Create(new DownloadOptions { TempFolder = root });
            var tempFiles = new TemporaryFileManager(options);
            var generator = new ThumbnailGenerator(
                tempFiles,
                Options.Create(new FfmpegOptions()),
                NullLogger<ThumbnailGenerator>.Instance);

            var result = await generator.GenerateAsync(
                Path.Combine(root, "missing.mp4"),
                Guid.NewGuid());

            Assert.False(result.Success);
            Assert.Equal("THUMBNAIL_SOURCE_MISSING", result.ErrorCode);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
