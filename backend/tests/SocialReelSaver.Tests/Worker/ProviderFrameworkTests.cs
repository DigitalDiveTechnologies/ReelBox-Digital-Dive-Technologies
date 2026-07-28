using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SocialReelSaver.Application.Abstractions.Downloading;
using SocialReelSaver.Application.Abstractions.Providers;
using SocialReelSaver.Domain.Enums;
using SocialReelSaver.Infrastructure.Providers;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Tests.Worker;

public sealed class ProviderFrameworkTests
{
    [Fact]
    public void Factory_CreatesInstagramAndFacebookProviders()
    {
        var factory = CreateFactory(CreateRealProviders());

        Assert.Equal(MediaPlatform.Instagram, factory.Create(MediaPlatform.Instagram).Platform);
        Assert.Equal(MediaPlatform.Facebook, factory.Create(MediaPlatform.Facebook).Platform);
    }

    [Fact]
    public void Resolver_SelectsProviderByPlatform()
    {
        var resolver = new MediaProviderResolver(CreateFactory(CreateRealProviders()));

        Assert.True(resolver.TryResolve(MediaPlatform.Instagram, out var ig));
        Assert.Equal(nameof(InstagramProvider), ig!.Name);
        Assert.True(resolver.TryResolve(MediaPlatform.Facebook, out var fb));
        Assert.Equal(nameof(FacebookProvider), fb!.Name);
    }

    [Fact]
    public void Factory_WhenDisabled_ReturnsUnavailable()
    {
        var options = new ProvidersOptions
        {
            Instagram = new ProviderPlatformOptions { Enabled = false },
        };
        var factory = CreateFactory(CreateRealProviders(options), options);

        Assert.False(factory.TryCreate(MediaPlatform.Instagram, out _));
        var ex = Assert.Throws<ProviderException>(() => factory.Create(MediaPlatform.Instagram));
        Assert.Equal(ProviderErrorCode.UnsupportedPlatform, ex.ErrorCode);
    }

    [Fact]
    public void ProviderErrorMapper_MapsToSrsCodes()
    {
        Assert.Equal("UNSUPPORTED_PLATFORM", ProviderErrorMapper.ToMediaErrorCode(ProviderErrorCode.UnsupportedPlatform));
        Assert.Equal("PROVIDER_TEMPORARY_FAILURE", ProviderErrorMapper.ToMediaErrorCode(ProviderErrorCode.TemporaryFailure));
        Assert.Equal("DOWNLOAD_TIMEOUT", ProviderErrorMapper.ToMediaErrorCode(ProviderErrorCode.ProviderTimeout));
        Assert.Equal("UNKNOWN", ProviderErrorMapper.ToMediaErrorCode(ProviderErrorCode.ProviderCancelled));
        Assert.Equal("UNKNOWN", ProviderErrorMapper.ToMediaErrorCode(ProviderErrorCode.InvalidProviderResponse));
        Assert.Equal("UNKNOWN", ProviderErrorMapper.ToMediaErrorCode(ProviderErrorCode.ConfigurationError));
        Assert.Equal("UNKNOWN", ProviderErrorMapper.ToMediaErrorCode(ProviderErrorCode.NotImplemented));
        Assert.Equal("ACCESS_NOT_PERMITTED", ProviderErrorMapper.ToMediaErrorCode(ProviderErrorCode.AccessNotPermitted));
        Assert.Equal("MEDIA_NOT_FOUND", ProviderErrorMapper.ToMediaErrorCode(ProviderErrorCode.MediaNotFound));
    }

    [Fact]
    public void ResultValidator_RejectsMissingOrInvalidUrl()
    {
        var resolver = CreateResolver(new ProvidersOptions());
        var opts = Options.Create(new ProvidersOptions { Resolver = "MetaGraph" });
        var validator = new ProviderResultValidator(resolver, opts);
        var provider = new InstagramProvider(
            resolver,
            CreateYtDlp(opts.Value),
            CreateRapidApi(opts.Value),
            opts);

        var missing = validator.Validate(ProviderResult.Ok(" "), provider);
        Assert.False(missing.Success);
        Assert.Equal(ProviderErrorCode.InvalidProviderResponse, missing.ErrorCode);

        var badScheme = validator.Validate(ProviderResult.Ok("ftp://example/file.mp4"), provider);
        Assert.False(badScheme.Success);
        Assert.Equal(ProviderErrorCode.InvalidProviderResponse, badScheme.ErrorCode);

        var disallowedHost = validator.Validate(ProviderResult.Ok("https://evil.example/a.mp4"), provider);
        Assert.False(disallowedHost.Success);
        Assert.Equal(ProviderErrorCode.AccessNotPermitted, disallowedHost.ErrorCode);

        var ok = validator.Validate(ProviderResult.Ok("https://scontent.cdninstagram.com/v/t.mp4"), provider);
        Assert.True(ok.Success);
    }

    [Fact]
    public async Task Executor_WhenOfficialApiLacksDownloadableSource_ReturnsAccessNotPermitted()
    {
        var handler = new ScriptedHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath.Contains("instagram_oembed", StringComparison.Ordinal))
            {
                return JsonResponse("""{"html":"<blockquote/>","provider_name":"Instagram","type":"rich","version":"1.0"}""");
            }

            return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
        });

        var providers = CreateRealProviders(httpHandler: handler);
        var executor = CreateExecutor(providers);
        var outcome = await executor.ExecuteAsync(CreateContext(MediaPlatform.Instagram));

        Assert.False(outcome.Result.Success);
        Assert.Equal(ProviderErrorCode.AccessNotPermitted, outcome.Result.ErrorCode);
        Assert.Equal("ACCESS_NOT_PERMITTED", outcome.Result.MediaErrorCode);
        Assert.False(outcome.Diagnostics.IsPlaceholder);
        Assert.Equal(nameof(InstagramProvider), outcome.Diagnostics.ProviderName);
    }

    [Fact]
    public async Task Executor_WhenGraphReturnsMediaUrl_ResolvesSuccessfully()
    {
        var handler = new ScriptedHandler(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.Contains("instagram_oembed", StringComparison.Ordinal))
            {
                return JsonResponse("""{"html":"<blockquote/>","author_name":"real_creator","title":"My Reel Caption","provider_name":"Instagram","type":"rich","version":"1.0"}""");
            }

            // Graph /?id= lookup
            return JsonResponse("""{"media_url":"https://scontent.cdninstagram.com/v/t51/video.mp4","title":"Graph Title"}""");
        });

        var options = new ProvidersOptions { AccessToken = "test-token" };
        var providers = CreateRealProviders(options, handler);
        var executor = CreateExecutor(providers, options);
        var outcome = await executor.ExecuteAsync(CreateContext(MediaPlatform.Instagram));

        Assert.True(outcome.Result.Success);
        Assert.Equal("https://scontent.cdninstagram.com/v/t51/video.mp4", outcome.Result.ResolvedSourceUrl);
        Assert.Equal("Graph Title", outcome.Result.Title);
        Assert.False(outcome.Diagnostics.IsPlaceholder);
    }

    [Fact]
    public async Task Executor_WhenGraphTitleMissing_PrefersOEmbedAuthorName()
    {
        var handler = new ScriptedHandler(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.Contains("instagram_oembed", StringComparison.Ordinal))
            {
                return JsonResponse("""{"html":"<blockquote/>","author_name":"travel_real","title":"caption text","provider_name":"Instagram","type":"rich","version":"1.0"}""");
            }

            return JsonResponse("""{"media_url":"https://scontent.cdninstagram.com/v/t51/video.mp4"}""");
        });

        var options = new ProvidersOptions { AccessToken = "test-token" };
        var providers = CreateRealProviders(options, handler);
        var executor = CreateExecutor(providers, options);
        var outcome = await executor.ExecuteAsync(CreateContext(MediaPlatform.Instagram));

        Assert.True(outcome.Result.Success);
        Assert.Equal("travel_real", outcome.Result.Title);
    }

    [Fact]
    public async Task Executor_WhenDisabled_ReturnsConfigurationError()
    {
        var options = new ProvidersOptions { Instagram = new ProviderPlatformOptions { Enabled = false } };
        var executor = CreateExecutor(CreateRealProviders(options), options);

        var outcome = await executor.ExecuteAsync(CreateContext(MediaPlatform.Instagram));

        Assert.False(outcome.Result.Success);
        Assert.Equal(ProviderErrorCode.ConfigurationError, outcome.Result.ErrorCode);
        Assert.Equal("UNKNOWN", outcome.Result.MediaErrorCode);
    }

    [Fact]
    public async Task Executor_Timeout_MapsToProviderTimeout()
    {
        var executor = CreateExecutor(
            [new SlowProvider()],
            new ProvidersOptions { TimeoutSeconds = 1, MaximumExecutionSeconds = 1 });

        var outcome = await executor.ExecuteAsync(CreateContext(MediaPlatform.Instagram));

        Assert.False(outcome.Result.Success);
        Assert.Equal(ProviderErrorCode.ProviderTimeout, outcome.Result.ErrorCode);
        Assert.True(outcome.Diagnostics.TimedOut);
        Assert.Equal("DOWNLOAD_TIMEOUT", outcome.Result.MediaErrorCode);
    }

    [Fact]
    public async Task Executor_Cancellation_MapsToProviderCancelled()
    {
        var executor = CreateExecutor(
            [new SlowProvider()],
            new ProvidersOptions { TimeoutSeconds = 30, MaximumExecutionSeconds = 30 });

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var outcome = await executor.ExecuteAsync(CreateContext(MediaPlatform.Instagram), cts.Token);

        Assert.False(outcome.Result.Success);
        Assert.Equal(ProviderErrorCode.ProviderCancelled, outcome.Result.ErrorCode);
        Assert.True(outcome.Diagnostics.Cancelled);
        Assert.Equal("UNKNOWN", outcome.Result.MediaErrorCode);
    }

    [Fact]
    public async Task Executor_SuccessfulProvider_PassesValidation()
    {
        var executor = CreateExecutor([new SuccessfulProvider()]);
        var outcome = await executor.ExecuteAsync(CreateContext(MediaPlatform.Instagram));

        Assert.True(outcome.Result.Success);
        Assert.Equal("https://scontent.cdninstagram.com/v/ok.mp4", outcome.Result.ResolvedSourceUrl);
        Assert.False(outcome.Diagnostics.IsPlaceholder);
        Assert.Null(outcome.Diagnostics.ErrorCode);
    }

    [Fact]
    public async Task Executor_RetryDisabled_ConvertsTemporaryToPermanent()
    {
        var executor = CreateExecutor(
            [new TemporaryFailureProvider()],
            new ProvidersOptions
            {
                Instagram = new ProviderPlatformOptions { Enabled = true, RetryEligible = false },
            });

        var outcome = await executor.ExecuteAsync(CreateContext(MediaPlatform.Instagram));

        Assert.Equal(ProviderErrorCode.PermanentFailure, outcome.Result.ErrorCode);
        Assert.Equal("UNKNOWN", outcome.Result.MediaErrorCode);
    }

    [Fact]
    public void Capabilities_RealProviders_AreProductionReady()
    {
        var ig = CreateRealProviders().OfType<InstagramProvider>().Single();
        Assert.True(ig.Capabilities.CanResolve);
        Assert.True(ig.Capabilities.SupportsCancellation);
        Assert.True(ig.Capabilities.SupportsTimeout);
        Assert.False(ig.Capabilities.IsPlaceholderImplementation);
    }

    private static MediaProviderFactory CreateFactory(
        IEnumerable<IMediaProvider> providers,
        ProvidersOptions? options = null) =>
        new(providers, Options.Create(options ?? new ProvidersOptions()));

    private static IMediaProviderExecutor CreateExecutor(
        IEnumerable<IMediaProvider> providers,
        ProvidersOptions? options = null)
    {
        var opts = Options.Create(options ?? new ProvidersOptions());
        var resolver = CreateResolver(opts.Value);
        return new MediaProviderExecutor(
            new MediaProviderResolver(CreateFactory(providers, options)),
            new ProviderResultValidator(resolver, opts),
            opts,
            NullLogger<MediaProviderExecutor>.Instance);
    }

    private static IReadOnlyList<IMediaProvider> CreateRealProviders(
        ProvidersOptions? options = null,
        HttpMessageHandler? httpHandler = null)
    {
        // Provider framework unit tests mock Meta Graph HTTP responses.
        var opts = CloneForMetaGraph(options ?? new ProvidersOptions());
        var meta = CreateResolver(opts, httpHandler);
        var ytDlp = CreateYtDlp(opts);
        var rapid = CreateRapidApi(opts);
        var o = Options.Create(opts);
        return [new InstagramProvider(meta, ytDlp, rapid, o), new FacebookProvider(meta, ytDlp, rapid, o)];
    }

    private static ProvidersOptions CloneForMetaGraph(ProvidersOptions source) => new()
    {
        TimeoutSeconds = source.TimeoutSeconds,
        MaximumExecutionSeconds = source.MaximumExecutionSeconds,
        GraphApiBaseUrl = source.GraphApiBaseUrl,
        AccessToken = source.AccessToken,
        AllowedResolvedHostSuffixes = source.AllowedResolvedHostSuffixes,
        Instagram = source.Instagram,
        Facebook = source.Facebook,
        Resolver = "MetaGraph",
        YtDlpExecutablePath = source.YtDlpExecutablePath,
        YtDlpTimeoutSeconds = source.YtDlpTimeoutSeconds,
    };

    private static YtDlpMediaResolver CreateYtDlp(ProvidersOptions options) =>
        new(
            new NoOpTemporaryFileManager(),
            Options.Create(options),
            NullLogger<YtDlpMediaResolver>.Instance);

    private static RapidApiMediaResolver CreateRapidApi(ProvidersOptions options) =>
        new(
            new TestHttpClientFactory(new ScriptedHandler(_ =>
                JsonResponse("""{"download_url":"https://example.com/v.mp4","thumb":"https://example.com/t.jpg","caption":"x"}"""))),
            Options.Create(new RapidApiOptions
            {
                BaseUrl = "https://full-downloader-social-media.p.rapidapi.com",
                Host = "full-downloader-social-media.p.rapidapi.com",
                ApiKey = "test-key",
            }),
            NullLogger<RapidApiMediaResolver>.Instance);

    private static MetaGraphMediaResolver CreateResolver(
        ProvidersOptions options,
        HttpMessageHandler? httpHandler = null)
    {
        var handler = httpHandler ?? new ScriptedHandler(_ =>
            JsonResponse("""{"html":"<blockquote/>","provider_name":"Instagram","type":"rich","version":"1.0"}"""));
        var factory = new TestHttpClientFactory(handler);
        return new MetaGraphMediaResolver(
            factory,
            Options.Create(options),
            NullLogger<MetaGraphMediaResolver>.Instance);
    }

    private sealed class NoOpTemporaryFileManager : ITemporaryFileManager
    {
        public string CreateTempFilePath(Guid mediaId, string? extension = null) =>
            Path.Combine(Path.GetTempPath(), $"{mediaId:N}{extension ?? ".bin"}");

        public Task CleanupAsync(string? path, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task CleanupMediaTempAsync(Guid mediaId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private static ProviderContext CreateContext(MediaPlatform platform) => new()
    {
        MediaId = Guid.NewGuid(),
        JobId = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Platform = platform,
        OriginalUrl = platform == MediaPlatform.Instagram
            ? "https://www.instagram.com/reel/AbC123/"
            : "https://www.facebook.com/reel/123456789/",
        Attempt = 0,
    };

    private static HttpResponseMessage JsonResponse(string json) =>
        new(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        };

    private sealed class SlowProvider : IMediaProvider
    {
        public string Name => nameof(SlowProvider);
        public MediaPlatform Platform => MediaPlatform.Instagram;
        public ProviderCapabilities Capabilities { get; } = ProviderCapabilities.ProductionReady();

        public async Task<ProviderResult> ExecuteAsync(
            ProviderContext context,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return ProviderResult.Ok("https://scontent.cdninstagram.com/v/late.mp4");
        }
    }

    private sealed class SuccessfulProvider : IMediaProvider
    {
        public string Name => nameof(SuccessfulProvider);
        public MediaPlatform Platform => MediaPlatform.Instagram;
        public ProviderCapabilities Capabilities { get; } = ProviderCapabilities.ProductionReady();

        public Task<ProviderResult> ExecuteAsync(
            ProviderContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ProviderResult.Ok("https://scontent.cdninstagram.com/v/ok.mp4", mimeType: "video/mp4"));
    }

    private sealed class TemporaryFailureProvider : IMediaProvider
    {
        public string Name => nameof(TemporaryFailureProvider);
        public MediaPlatform Platform => MediaPlatform.Instagram;
        public ProviderCapabilities Capabilities { get; } = ProviderCapabilities.ProductionReady();

        public Task<ProviderResult> ExecuteAsync(
            ProviderContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ProviderResult.Failed(ProviderErrorCode.TemporaryFailure, "transient"));
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
}
