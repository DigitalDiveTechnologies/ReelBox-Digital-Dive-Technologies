using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SocialReelSaver.Application.Abstractions.Downloading;
using SocialReelSaver.Application.Abstractions.Media;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Abstractions.Providers;
using SocialReelSaver.Application.Abstractions.Queue;
using SocialReelSaver.Application.Abstractions.Storage;
using SocialReelSaver.Application.Media.Jobs;
using SocialReelSaver.Application.Media.Retry;
using SocialReelSaver.Domain.Entities;
using SocialReelSaver.Domain.Enums;
using SocialReelSaver.Infrastructure.Downloading;
using SocialReelSaver.Infrastructure.Media;
using SocialReelSaver.Infrastructure.Providers;
using SocialReelSaver.Infrastructure.Queue;
using SocialReelSaver.Infrastructure.Storage;
using SocialReelSaver.Infrastructure.Workers;
using SocialReelSaver.Shared.Configuration;
using SocialReelSaver.Tests;

namespace SocialReelSaver.Tests.Worker;

public sealed class MediaDownloadPipelineTests
{
    [Fact]
    public async Task ExecuteAsync_WhenTemporaryProviderFailure_SchedulesRetryWithNextRetryAt()
    {
        var item = CreateQueuedItem(MediaPlatform.Instagram);
        var root = Path.Combine(Path.GetTempPath(), "srs-retry-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var repo = new FakeMediaRepository(item);
            var status = new MediaStatusService(repo);
            var publisher = new RecordingPublisher();
            var downloadOptions = Options.Create(new DownloadOptions { TempFolder = root });
            var storageOptions = Options.Create(new ObjectStorageOptions
            {
                Provider = "Local",
                LocalRootPath = root,
            });
            var tempFiles = new TemporaryFileManager(downloadOptions);
            var localStorage = new LocalObjectStorageService(
                storageOptions,
                NullLogger<LocalObjectStorageService>.Instance);

            var pipeline = new MediaDownloadPipeline(
                repo,
                status,
                CreateExecutor([new TemporaryFailureStubProvider()]),
                new StubDownloader(tempFiles),
                new DownloadValidator(downloadOptions),
                new ThumbnailGenerator(
                    tempFiles,
                    Options.Create(new FfmpegOptions { ExecutablePath = "__srs_ffmpeg_missing__" }),
                    NullLogger<ThumbnailGenerator>.Instance),
                new FixedStorageFactory(localStorage),
                tempFiles,
                new ExponentialBackoffRetryPolicy(Options.Create(new WorkerOptions
                {
                    MaxRetries = 3,
                    BaseBackoffSeconds = 2,
                    MaxBackoffSeconds = 60,
                })),
                publisher,
                new MediaCategorizationQueue(),
                storageOptions,
                NullLogger<MediaDownloadPipeline>.Instance);

            var before = DateTimeOffset.UtcNow;
            await pipeline.ExecuteAsync(CreateJob(item));

            Assert.Equal(MediaStatus.Queued, item.Status);
            Assert.Equal(1, item.RetryCount);
            Assert.NotNull(item.NextRetryAt);
            Assert.True(item.NextRetryAt >= before.AddSeconds(2));
            Assert.Equal("PROVIDER_TEMPORARY_FAILURE", item.ErrorCode);
            Assert.Single(publisher.Jobs);
            Assert.Equal(item.Id, publisher.Jobs[0].MediaId);
            Assert.Equal(item.NextRetryAt, publisher.Jobs[0].NotBefore);
            Assert.Equal(1, publisher.Jobs[0].Attempt);
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
    public async Task ExecuteAsync_WhenProviderCannotResolveDownloadableSource_MarksFailed()
    {
        var item = CreateQueuedItem(MediaPlatform.Instagram);
        var handler = new ScriptedHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"html":"<blockquote/>","provider_name":"Instagram","type":"rich","version":"1.0"}""",
                System.Text.Encoding.UTF8,
                "application/json"),
        });
        var metaOpts = Options.Create(new ProvidersOptions { Resolver = "MetaGraph" });
        var meta = new MetaGraphMediaResolver(
            new TestHttpClientFactory(handler),
            metaOpts,
            NullLogger<MetaGraphMediaResolver>.Instance);
        var ytDlp = new YtDlpMediaResolver(
            new PipelineTestTempFiles(),
            metaOpts,
            NullLogger<YtDlpMediaResolver>.Instance);
        var pipeline = CreatePipeline(
            item,
            CreateExecutor([
                new InstagramProvider(meta, ytDlp, metaOpts),
                new FacebookProvider(meta, ytDlp, metaOpts),
            ]));

        await pipeline.ExecuteAsync(CreateJob(item));

        Assert.Equal(MediaStatus.Failed, item.Status);
        Assert.Equal("ACCESS_NOT_PERMITTED", item.ErrorCode);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDownloadSucceeds_CompletesWithLocalStorage()
    {
        var root = Path.Combine(Path.GetTempPath(), "srs-b8-pipeline-" + Guid.NewGuid().ToString("N"));
        var temp = Path.Combine(root, "temp");
        var storage = Path.Combine(root, "storage");
        Directory.CreateDirectory(temp);
        Directory.CreateDirectory(storage);

        try
        {
            var item = CreateQueuedItem(MediaPlatform.Instagram);
            var repo = new FakeMediaRepository(item);
            var status = new MediaStatusService(repo);
            var queue = new InMemoryMediaJobQueue();
            var publisher = new MediaJobPublisher(queue);
            var downloadOptions = Options.Create(new DownloadOptions
            {
                TempFolder = temp,
                MaxFileSizeBytes = 1024 * 1024,
                TimeoutSeconds = 30,
            });
            var storageOptions = Options.Create(new ObjectStorageOptions
            {
                Provider = "Local",
                LocalRootPath = storage,
                UploadTimeoutSeconds = 30,
            });

            var tempFiles = new TemporaryFileManager(downloadOptions);
            var validator = new DownloadValidator(downloadOptions);
            var localStorage = new LocalObjectStorageService(
                storageOptions,
                NullLogger<LocalObjectStorageService>.Instance);
            var factory = new FixedStorageFactory(localStorage);
            var downloader = new StubDownloader(tempFiles);
            var executor = CreateExecutor([new StubProvider(MediaPlatform.Instagram)]);
            var retry = new ExponentialBackoffRetryPolicy(Options.Create(new WorkerOptions
            {
                MaxRetries = 3,
                BaseBackoffSeconds = 1,
                MaxBackoffSeconds = 10,
            }));

            var pipeline = new MediaDownloadPipeline(
                repo,
                status,
                executor,
                downloader,
                validator,
                new ThumbnailGenerator(
                    tempFiles,
                    Options.Create(new FfmpegOptions { ExecutablePath = "__srs_ffmpeg_missing__" }),
                    NullLogger<ThumbnailGenerator>.Instance),
                factory,
                tempFiles,
                retry,
                publisher,
                new MediaCategorizationQueue(),
                storageOptions,
                NullLogger<MediaDownloadPipeline>.Instance);

            await pipeline.ExecuteAsync(CreateJob(item));

            Assert.Equal(MediaStatus.Completed, item.Status);
            Assert.False(string.IsNullOrWhiteSpace(item.MediaStorageKey));
            Assert.Equal("video/mp4", item.MimeType);
            Assert.True(item.FileSizeBytes > 0);
            Assert.True(await localStorage.ExistsAsync(item.MediaStorageKey!));
            Assert.Null(item.ErrorCode);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static MediaDownloadPipeline CreatePipeline(MediaItem item, IMediaProviderExecutor executor)
    {
        var root = Path.Combine(Path.GetTempPath(), "srs-b8-empty-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var repo = new FakeMediaRepository(item);
        var status = new MediaStatusService(repo);
        var queue = new InMemoryMediaJobQueue();
        var publisher = new MediaJobPublisher(queue);
        var downloadOptions = Options.Create(new DownloadOptions { TempFolder = root });
        var storageOptions = Options.Create(new ObjectStorageOptions
        {
            Provider = "Local",
            LocalRootPath = root,
        });
        var tempFiles = new TemporaryFileManager(downloadOptions);
        var localStorage = new LocalObjectStorageService(
            storageOptions,
            NullLogger<LocalObjectStorageService>.Instance);

        return new MediaDownloadPipeline(
            repo,
            status,
            executor,
            new StubDownloader(tempFiles),
            new DownloadValidator(downloadOptions),
            new ThumbnailGenerator(
                tempFiles,
                Options.Create(new FfmpegOptions { ExecutablePath = "__srs_ffmpeg_missing__" }),
                NullLogger<ThumbnailGenerator>.Instance),
            new FixedStorageFactory(localStorage),
            tempFiles,
            new ExponentialBackoffRetryPolicy(Options.Create(new WorkerOptions
            {
                MaxRetries = 3,
                BaseBackoffSeconds = 1,
                MaxBackoffSeconds = 10,
            })),
                publisher,
                new MediaCategorizationQueue(),
                storageOptions,
                NullLogger<MediaDownloadPipeline>.Instance);
    }

    internal static IMediaProviderExecutor CreateExecutor(
        IEnumerable<IMediaProvider> providers,
        ProvidersOptions? options = null)
    {
        var opts = Options.Create(options ?? new ProvidersOptions());
        var operational = new TestOperationalSettings();
        var factory = new MediaProviderFactory(providers, opts, operational);
        var resolver = new MediaProviderResolver(factory);
        var meta = new MetaGraphMediaResolver(
            new TestHttpClientFactory(new ScriptedHandler(_ =>
                new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json"),
                })),
            opts,
            NullLogger<MetaGraphMediaResolver>.Instance);
        return new MediaProviderExecutor(
            resolver,
            new ProviderResultValidator(meta, opts),
            opts,
            operational,
            NullLogger<MediaProviderExecutor>.Instance);
    }

    private static MediaItem CreateQueuedItem(MediaPlatform platform) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        OriginalUrl = platform == MediaPlatform.Instagram
            ? "https://instagram.com/reel/abc"
            : "https://facebook.com/reel/abc",
        Platform = platform,
        Status = MediaStatus.Queued,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private static MediaDownloadJob CreateJob(MediaItem item) => new()
    {
        MediaId = item.Id,
        UserId = item.UserId,
        Platform = item.Platform,
        OriginalUrl = item.OriginalUrl,
        Attempt = 0,
    };

    private sealed class TemporaryFailureStubProvider : IMediaProvider
    {
        public string Name => nameof(TemporaryFailureStubProvider);

        public MediaPlatform Platform => MediaPlatform.Instagram;

        public ProviderCapabilities Capabilities { get; } = ProviderCapabilities.ProductionReady();

        public Task<ProviderResult> ExecuteAsync(
            ProviderContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ProviderResult.Failed(
                ProviderErrorCode.TemporaryFailure,
                "transient provider failure"));
    }

    private sealed class RecordingPublisher : IMediaJobPublisher
    {
        public List<MediaDownloadJob> Jobs { get; } = [];

        public Task PublishDownloadJobAsync(
            MediaDownloadJob job,
            CancellationToken cancellationToken = default)
        {
            Jobs.Add(job);
            return Task.CompletedTask;
        }
    }

    private sealed class StubProvider : IMediaProvider
    {
        public StubProvider(MediaPlatform platform) => Platform = platform;

        public string Name => "StubProvider";

        public MediaPlatform Platform { get; }

        public ProviderCapabilities Capabilities { get; } = ProviderCapabilities.ProductionReady();

        public Task<ProviderResult> ExecuteAsync(
            ProviderContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ProviderResult.Ok(
                "https://scontent.cdninstagram.com/v/media.mp4",
                title: "Stub reel",
                mimeType: "video/mp4",
                extension: ".mp4",
                durationMs: 1500));
    }

    private sealed class ScriptedHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public ScriptedHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
            _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(_responder(request));
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public TestHttpClientFactory(HttpMessageHandler handler) => _handler = handler;

        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }

    private sealed class StubDownloader : IMediaDownloader
    {
        private readonly ITemporaryFileManager _tempFiles;

        public StubDownloader(ITemporaryFileManager tempFiles) => _tempFiles = tempFiles;

        public async Task<MediaDownloadResult> DownloadAsync(
            DownloadContext context,
            CancellationToken cancellationToken = default)
        {
            var path = _tempFiles.CreateTempFilePath(context.MediaId, ".mp4");
            var bytes = new byte[72];
            "ftypisom"u8.CopyTo(bytes);
            await File.WriteAllBytesAsync(path, bytes, cancellationToken);
            return MediaDownloadResult.Ok(path, "video/mp4", bytes.Length);
        }
    }

    private sealed class FixedStorageFactory : IObjectStorageFactory
    {
        private readonly IObjectStorageService _service;

        public FixedStorageFactory(IObjectStorageService service) => _service = service;

        public IObjectStorageService Create() => _service;

        public IObjectStorageService Create(string providerName) => _service;
    }

    private sealed class FakeMediaRepository : IMediaRepository
    {
        private readonly MediaItem _item;

        public FakeMediaRepository(MediaItem item) => _item = item;

        public Task AddAsync(MediaItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(MediaItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<MediaItem?> GetByIdAsync(Guid mediaId, CancellationToken cancellationToken = default) =>
            Task.FromResult<MediaItem?>(_item.Id == mediaId ? _item : null);

        public Task<MediaItem?> GetByIdWithUserAsync(Guid mediaId, CancellationToken cancellationToken = default) =>
            GetByIdAsync(mediaId, cancellationToken);

        public Task<MediaItem?> GetByIdForUserAsync(Guid mediaId, Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<MediaItem?>(_item.Id == mediaId && _item.UserId == userId ? _item : null);

        public Task<MediaItem?> GetByNormalizedUrlAsync(Guid userId, string normalizedUrl, CancellationToken cancellationToken = default) =>
            Task.FromResult<MediaItem?>(null);

        public Task<(IReadOnlyList<MediaItem> Items, int TotalCount)> ListForUserAsync(
            Guid userId,
            int page,
            int pageSize,
            MediaStatus? status,
            MediaPlatform? platform,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<MediaItem>, int)>((Array.Empty<MediaItem>(), 0));

        public Task<(IReadOnlyList<MediaItem> Items, int TotalCount)> ListAdminAsync(
            int page, int pageSize, string? search, MediaStatus? status, MediaPlatform? platform,
            Guid? userId, IReadOnlyList<MediaStatus>? statusIn, string? sortBy = null, string? sortDir = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<MediaItem>, int)>((Array.Empty<MediaItem>(), 0));

        public Task<IReadOnlyDictionary<MediaStatus, int>> StatusCountsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<MediaStatus, int>>(new Dictionary<MediaStatus, int>());

        public Task<long> SumFileSizeBytesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0L);

        public Task<IReadOnlyList<(string? MediaStorageKey, string? ThumbnailStorageKey)>> ListStorageKeysAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<(string?, string?)>>([]);

        public Task<IReadOnlyList<MediaItem>> ListStaleActiveAsync(
            DateTimeOffset staleBeforeUtc,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MediaItem>>([]);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(MediaItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class PipelineTestTempFiles : ITemporaryFileManager
    {
        public string CreateTempFilePath(Guid mediaId, string? extension = null) =>
            Path.Combine(Path.GetTempPath(), $"{mediaId:N}{extension ?? ".bin"}");

        public Task CleanupAsync(string? path, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task CleanupMediaTempAsync(Guid mediaId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
