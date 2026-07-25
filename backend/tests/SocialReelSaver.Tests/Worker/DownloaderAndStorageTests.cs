using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SocialReelSaver.Application.Abstractions.Storage;
using SocialReelSaver.Infrastructure.Downloading;
using SocialReelSaver.Infrastructure.Storage;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Tests.Worker;

public sealed class LocalObjectStorageTests
{
    [Fact]
    public async Task Upload_Exists_OpenRead_Delete_RoundTrip()
    {
        var root = Path.Combine(Path.GetTempPath(), "srs-b7-storage-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sut = new LocalObjectStorageService(
                Options.Create(new ObjectStorageOptions { LocalRootPath = root }),
                NullLogger<LocalObjectStorageService>.Instance);

            var key = "user/media/sample.mp4";
            await using (var content = new MemoryStream("hello-storage"u8.ToArray()))
            {
                var upload = await sut.UploadAsync(new StorageUploadRequest
                {
                    Key = key,
                    Content = content,
                    ContentType = "video/mp4",
                });
                Assert.True(upload.Success);
            }

            Assert.True(await sut.ExistsAsync(key));
            {
                await using var read = await sut.OpenReadAsync(key);
                Assert.NotNull(read);
                using var reader = new StreamReader(read!);
                Assert.Equal("hello-storage", await reader.ReadToEndAsync());
            }

            var delete = await sut.DeleteAsync(key);
            Assert.True(delete.Success);
            Assert.False(await sut.ExistsAsync(key));
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

public sealed class DownloadValidatorTests
{
    [Fact]
    public async Task ValidateAsync_AcceptsExistingMp4SizedFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "srs-b7-val-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "clip.mp4");
        await File.WriteAllBytesAsync(path, new byte[128]);

        try
        {
            var sut = new DownloadValidator(Options.Create(new DownloadOptions
            {
                MaxFileSizeBytes = 1024,
                TempFolder = root,
            }));

            var result = await sut.ValidateAsync(path, "video/mp4");
            Assert.True(result.Success);
            Assert.Equal("video/mp4", result.MimeType);
            Assert.Equal(128, result.FileSizeBytes);
            Assert.True(result.DurationIsPlaceholder);
            Assert.True(result.ChecksumIsPlaceholder);
            Assert.False(string.IsNullOrWhiteSpace(result.ChecksumSha256));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ValidateAsync_RejectsMissingFile()
    {
        var sut = new DownloadValidator(Options.Create(new DownloadOptions()));
        var result = await sut.ValidateAsync(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".mp4"));
        Assert.False(result.Success);
        Assert.Equal("INVALID_MEDIA", result.ErrorCode);
    }

    [Fact]
    public async Task ValidateAsync_RejectsOversizedFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "srs-b7-val2-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "big.mp4");
        await File.WriteAllBytesAsync(path, new byte[64]);

        try
        {
            var sut = new DownloadValidator(Options.Create(new DownloadOptions
            {
                MaxFileSizeBytes = 16,
            }));

            var result = await sut.ValidateAsync(path, "video/mp4");
            Assert.False(result.Success);
            Assert.Equal("FILE_TOO_LARGE", result.ErrorCode);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

public sealed class MediaDownloaderTests
{
    [Fact]
    public async Task DownloadAsync_StreamsHttpContentToTempFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "srs-b7-dl-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var handler = new StubHttpHandler(new byte[] { 1, 2, 3, 4, 5 }, "video/mp4");
            var http = new HttpClient(handler);
            var options = Options.Create(new DownloadOptions
            {
                TempFolder = root,
                MaxFileSizeBytes = 1024,
                TimeoutSeconds = 10,
            });
            var temp = new TemporaryFileManager(options);
            var sut = new MediaDownloader(http, temp, options, NullLogger<MediaDownloader>.Instance);

            var result = await sut.DownloadAsync(new Application.Abstractions.Downloading.DownloadContext
            {
                MediaId = Guid.NewGuid(),
                JobId = Guid.NewGuid(),
                SourceUrl = "https://cdn.example/test.mp4",
                SuggestedMimeType = "video/mp4",
            });

            Assert.True(result.Success);
            Assert.Equal(5, result.BytesDownloaded);
            Assert.True(File.Exists(result.LocalFilePath));
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, await File.ReadAllBytesAsync(result.LocalFilePath!));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task DownloadAsync_EnforcesMaxSize()
    {
        var root = Path.Combine(Path.GetTempPath(), "srs-b7-dl2-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var handler = new StubHttpHandler(new byte[32], "video/mp4");
            var http = new HttpClient(handler);
            var options = Options.Create(new DownloadOptions
            {
                TempFolder = root,
                MaxFileSizeBytes = 8,
                TimeoutSeconds = 10,
            });
            var sut = new MediaDownloader(
                http,
                new TemporaryFileManager(options),
                options,
                NullLogger<MediaDownloader>.Instance);

            var result = await sut.DownloadAsync(new Application.Abstractions.Downloading.DownloadContext
            {
                MediaId = Guid.NewGuid(),
                JobId = Guid.NewGuid(),
                SourceUrl = "https://cdn.example/big.mp4",
            });

            Assert.False(result.Success);
            Assert.Equal("FILE_TOO_LARGE", result.ErrorCode);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class StubHttpHandler : HttpMessageHandler
    {
        private readonly byte[] _payload;
        private readonly string _contentType;

        public StubHttpHandler(byte[] payload, string contentType)
        {
            _payload = payload;
            _contentType = contentType;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(_payload),
            };
            response.Content.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(_contentType);
            response.Content.Headers.ContentLength = _payload.Length;
            return Task.FromResult(response);
        }
    }
}
