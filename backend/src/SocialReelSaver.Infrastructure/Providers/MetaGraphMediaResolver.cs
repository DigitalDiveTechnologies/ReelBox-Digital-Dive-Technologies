using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialReelSaver.Application.Abstractions.Providers;
using SocialReelSaver.Domain.Enums;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Infrastructure.Providers;

/// <summary>
/// Resolves media sources using Meta Graph API only (SRS FR-007 / §16).
/// Does not scrape pages, drive browsers, or use unofficial download tools.
/// </summary>
public sealed class MetaGraphMediaResolver
{
    public const string HttpClientName = nameof(MetaGraphMediaResolver);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ProvidersOptions _options;
    private readonly ILogger<MetaGraphMediaResolver> _logger;

    public MetaGraphMediaResolver(
        IHttpClientFactory httpClientFactory,
        IOptions<ProvidersOptions> options,
        ILogger<MetaGraphMediaResolver> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    private HttpClient CreateClient() => _httpClientFactory.CreateClient(HttpClientName);

    public async Task<ProviderResult> ResolveAsync(
        MediaPlatform platform,
        string originalUrl,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out var sourceUri) ||
            (sourceUri.Scheme != Uri.UriSchemeHttps && sourceUri.Scheme != Uri.UriSchemeHttp))
        {
            return ProviderResult.Failed(ProviderErrorCode.InvalidProviderResponse, "Original URL is not a valid absolute HTTP(S) URL.");
        }

        if (!IsSupportedContentUrl(platform, sourceUri))
        {
            return ProviderResult.Failed(
                ProviderErrorCode.MediaNotFound,
                "URL does not match a supported Instagram/Facebook media path.");
        }

        // 1) Confirm the public object is reachable via official oEmbed (token optional per Meta policy).
        var oembed = await QueryOEmbedAsync(platform, sourceUri, cancellationToken);
        if (!oembed.Success)
        {
            return oembed.Failure!;
        }

        // Prefer creator/channel identity from oEmbed author_name over generic title/caption.
        var displayTitle = FirstNonEmpty(oembed.AuthorName, oembed.Title);

        // 2) Attempt official Graph lookup for a downloadable media_url/source when available.
        var graphMedia = await QueryGraphMediaUrlAsync(sourceUri, cancellationToken);
        if (graphMedia.Success && !string.IsNullOrWhiteSpace(graphMedia.MediaUrl))
        {
            if (!IsAllowedResolvedHost(graphMedia.MediaUrl))
            {
                _logger.LogWarning(
                    "Rejected resolved media URL host outside allowlist for {Url}",
                    originalUrl);
                return ProviderResult.Failed(
                    ProviderErrorCode.AccessNotPermitted,
                    "Resolved media host is not on the approved platform CDN allowlist.") with
                {
                    Title = displayTitle,
                };
            }

            return ProviderResult.Ok(
                graphMedia.MediaUrl!,
                title: FirstNonEmpty(graphMedia.Title, displayTitle),
                mimeType: GuessMime(graphMedia.MediaUrl!),
                extension: GuessExtension(graphMedia.MediaUrl!));
        }

        if (graphMedia.Failure is not null &&
            graphMedia.Failure.ErrorCode is ProviderErrorCode.TemporaryFailure or ProviderErrorCode.ProviderTimeout)
        {
            return graphMedia.Failure with { Title = displayTitle };
        }

        // Official Meta APIs do not expose downloadable media binaries for arbitrary third-party posts.
        // SRS §16 forbids bypassing access controls / DRM / private-account protections.
        return ProviderResult.Failed(
            ProviderErrorCode.AccessNotPermitted,
            "Official Meta Graph APIs did not provide a downloadable media source for this content. " +
            "The system will not bypass platform access controls to extract media.") with
        {
            Title = displayTitle,
        };
    }

    private async Task<OEmbedProbe> QueryOEmbedAsync(
        MediaPlatform platform,
        Uri sourceUri,
        CancellationToken cancellationToken)
    {
        var endpoint = platform == MediaPlatform.Instagram ? "instagram_oembed" : "oembed_video";
        var url = BuildGraphUrl(endpoint, new Dictionary<string, string?>
        {
            ["url"] = sourceUri.ToString(),
            ["omitscript"] = "true",
            ["access_token"] = string.IsNullOrWhiteSpace(_options.AccessToken) ? null : _options.AccessToken,
        });

        try
        {
            using var response = await CreateClient().GetAsync(url, cancellationToken);
            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
            {
                return OEmbedProbe.Fail(ProviderResult.Failed(
                    ProviderErrorCode.MediaNotFound,
                    "Media was not found or is not available through the official oEmbed API."));
            }

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return OEmbedProbe.Fail(ProviderResult.Failed(
                    ProviderErrorCode.AccessNotPermitted,
                    "Official oEmbed API denied access to this media."));
            }

            if ((int)response.StatusCode == 429 || (int)response.StatusCode >= 500)
            {
                return OEmbedProbe.Fail(ProviderResult.Failed(
                    ProviderErrorCode.TemporaryFailure,
                    "Official oEmbed API is temporarily unavailable."));
            }

            if (!response.IsSuccessStatusCode)
            {
                return OEmbedProbe.Fail(ProviderResult.Failed(
                    ProviderErrorCode.TemporaryFailure,
                    $"Official oEmbed API returned HTTP {(int)response.StatusCode}."));
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = doc.RootElement;
            var title = TryGetString(root, "title");
            var authorName = TryGetString(root, "author_name");

            return OEmbedProbe.Ok(title, authorName);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "oEmbed network failure for {Url}", sourceUri);
            return OEmbedProbe.Fail(ProviderResult.Failed(
                ProviderErrorCode.TemporaryFailure,
                "Network error while calling the official oEmbed API."));
        }
    }

    private async Task<GraphMediaProbe> QueryGraphMediaUrlAsync(
        Uri sourceUri,
        CancellationToken cancellationToken)
    {
        // Graph node lookup requires an access token.
        if (string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            return GraphMediaProbe.Empty();
        }

        var url = BuildGraphUrl(string.Empty, new Dictionary<string, string?>
        {
            ["id"] = sourceUri.ToString(),
            ["fields"] = "og_object{id,title,description},source,media_url,format",
            ["access_token"] = _options.AccessToken,
        });

        try
        {
            using var response = await CreateClient().GetAsync(url, cancellationToken);
            if (response.StatusCode is HttpStatusCode.NotFound)
            {
                return GraphMediaProbe.Empty();
            }

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return GraphMediaProbe.Fail(ProviderResult.Failed(
                    ProviderErrorCode.AccessNotPermitted,
                    "Graph API denied access while resolving media."));
            }

            if ((int)response.StatusCode == 429 || (int)response.StatusCode >= 500)
            {
                return GraphMediaProbe.Fail(ProviderResult.Failed(
                    ProviderErrorCode.TemporaryFailure,
                    "Graph API is temporarily unavailable."));
            }

            if (!response.IsSuccessStatusCode)
            {
                return GraphMediaProbe.Empty();
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = doc.RootElement;

            var title = TryGetString(root, "title");
            if (root.TryGetProperty("og_object", out var og) && og.ValueKind == JsonValueKind.Object)
            {
                title ??= TryGetString(og, "title");
            }

            var mediaUrl =
                TryGetString(root, "media_url")
                ?? TryGetString(root, "source")
                ?? TryGetFormatUrl(root);

            if (string.IsNullOrWhiteSpace(mediaUrl))
            {
                return GraphMediaProbe.Empty(title);
            }

            return GraphMediaProbe.Ok(mediaUrl, title);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Graph media lookup network failure for {Url}", sourceUri);
            return GraphMediaProbe.Fail(ProviderResult.Failed(
                ProviderErrorCode.TemporaryFailure,
                "Network error while calling the official Graph API."));
        }
    }

    private Uri BuildGraphUrl(string relativePath, Dictionary<string, string?> query)
    {
        var baseUrl = _options.GraphApiBaseUrl.TrimEnd('/');
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var graphBase) ||
            !graphBase.Host.Equals("graph.facebook.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new ProviderException(
                ProviderErrorCode.ConfigurationError,
                "Providers:GraphApiBaseUrl must use the official graph.facebook.com host.",
                providerName: nameof(MetaGraphMediaResolver));
        }

        var path = string.IsNullOrWhiteSpace(relativePath) ? graphBase.AbsolutePath.TrimEnd('/') : $"{graphBase.AbsolutePath.TrimEnd('/')}/{relativePath.TrimStart('/')}";
        if (string.IsNullOrWhiteSpace(path) || path == "/")
        {
            path = string.IsNullOrWhiteSpace(relativePath) ? "/" : "/" + relativePath.TrimStart('/');
        }

        var builder = new UriBuilder(graphBase.Scheme, graphBase.Host, graphBase.Port, path);
        var pairs = query
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}");
        builder.Query = string.Join('&', pairs);
        return builder.Uri;
    }

    public bool IsAllowedResolvedHost(string mediaUrl)
    {
        if (!Uri.TryCreate(mediaUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return false;
        }

        var host = uri.Host;
        foreach (var suffix in _options.AllowedResolvedHostSuffixes)
        {
            if (string.IsNullOrWhiteSpace(suffix))
            {
                continue;
            }

            if (host.Equals(suffix.TrimStart('.'), StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith(suffix.StartsWith('.') ? suffix : "." + suffix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSupportedContentUrl(MediaPlatform platform, Uri uri)
    {
        var path = uri.AbsolutePath.TrimEnd('/');
        if (platform == MediaPlatform.Instagram)
        {
            return Regex.IsMatch(path, @"^/(reel|p|tv)/[^/]+$", RegexOptions.IgnoreCase);
        }

        // Facebook reel / watch / videos / share paths.
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

    private static string? TryGetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;

    private static string? TryGetFormatUrl(JsonElement root)
    {
        if (!root.TryGetProperty("format", out var format) || format.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var item in format.EnumerateArray())
        {
            var url = TryGetString(item, "url") ?? TryGetString(item, "picture");
            if (!string.IsNullOrWhiteSpace(url) &&
                (url.Contains(".mp4", StringComparison.OrdinalIgnoreCase) ||
                 url.Contains("video", StringComparison.OrdinalIgnoreCase)))
            {
                return url;
            }
        }

        return null;
    }

    private static string GuessMime(string url) =>
        url.Contains(".mp4", StringComparison.OrdinalIgnoreCase) ? "video/mp4" :
        url.Contains(".mov", StringComparison.OrdinalIgnoreCase) ? "video/quicktime" :
        "video/mp4";

    private static string GuessExtension(string url) =>
        url.Contains(".mov", StringComparison.OrdinalIgnoreCase) ? ".mov" : ".mp4";

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private sealed record OEmbedProbe(bool Success, string? Title, string? AuthorName, ProviderResult? Failure)
    {
        public static OEmbedProbe Ok(string? title, string? authorName = null) =>
            new(true, title, authorName, null);

        public static OEmbedProbe Fail(ProviderResult failure) => new(false, null, null, failure);
    }

    private sealed record GraphMediaProbe(bool Success, string? MediaUrl, string? Title, ProviderResult? Failure)
    {
        public static GraphMediaProbe Ok(string mediaUrl, string? title) => new(true, mediaUrl, title, null);
        public static GraphMediaProbe Empty(string? title = null) => new(false, null, title, null);
        public static GraphMediaProbe Fail(ProviderResult failure) => new(false, null, null, failure);
    }
}
