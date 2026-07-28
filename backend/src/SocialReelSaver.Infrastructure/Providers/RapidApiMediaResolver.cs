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
/// Resolves Instagram/Facebook media via Full Downloader Social Media (RapidAPI).
/// Returns a downloadable HTTPS URL; the pipeline then fetches bytes into local storage.
/// </summary>
public sealed class RapidApiMediaResolver
{
    public const string HttpClientName = "RapidApiMediaResolver";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly string[] ThumbnailPropertyNames =
    [
        "thumb",
        "thumbnail",
        "thumbnail_url",
        "thumbnailUrl",
        "cover",
        "cover_url",
        "coverUrl",
        "picture",
        "image",
        "image_url",
        "imageUrl",
        "poster",
        "og_image",
        "ogImage",
        "thumbnail_src",
        "thumbnailSrc",
    ];

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RapidApiOptions _options;
    private readonly ILogger<RapidApiMediaResolver> _logger;

    public RapidApiMediaResolver(
        IHttpClientFactory httpClientFactory,
        IOptions<RapidApiOptions> options,
        ILogger<RapidApiMediaResolver> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ProviderResult> ResolveAsync(
        MediaPlatform platform,
        string originalUrl,
        Guid mediaId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return ProviderResult.Failed(
                ProviderErrorCode.ConfigurationError,
                "RapidApi:ApiKey is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.BaseUrl) || string.IsNullOrWhiteSpace(_options.Host))
        {
            return ProviderResult.Failed(
                ProviderErrorCode.ConfigurationError,
                "RapidApi:BaseUrl / RapidApi:Host are not configured.");
        }

        if (!Uri.TryCreate(originalUrl.Trim(), UriKind.Absolute, out var sourceUri) ||
            (sourceUri.Scheme != Uri.UriSchemeHttps && sourceUri.Scheme != Uri.UriSchemeHttp))
        {
            return ProviderResult.Failed(
                ProviderErrorCode.InvalidProviderResponse,
                "Original media URL must be an absolute http(s) link.");
        }

        Uri requestUri;
        try
        {
            var baseUri = new Uri(_options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
            requestUri = new Uri(baseUri, "?url=" + Uri.EscapeDataString(sourceUri.ToString()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invalid RapidAPI base URL for media {MediaId}", mediaId);
            return ProviderResult.Failed(
                ProviderErrorCode.ConfigurationError,
                "RapidApi:BaseUrl is invalid.");
        }

        var client = _httpClientFactory.CreateClient(HttpClientName);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.TryAddWithoutValidation("x-rapidapi-host", _options.Host);
            request.Headers.TryAddWithoutValidation("x-rapidapi-key", _options.ApiKey);

            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests ||
                (int)response.StatusCode == 429)
            {
                return ProviderResult.Failed(
                    ProviderErrorCode.TemporaryFailure,
                    "RapidAPI rate limit exceeded. Try again later.");
            }

            if ((int)response.StatusCode >= 500)
            {
                return ProviderResult.Failed(
                    ProviderErrorCode.TemporaryFailure,
                    $"RapidAPI temporary failure (HTTP {(int)response.StatusCode}).");
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "RapidAPI non-success for media {MediaId} platform {Platform}: HTTP {Status} body={Body}",
                    mediaId,
                    platform,
                    (int)response.StatusCode,
                    Truncate(body, 400));

                return ProviderResult.Failed(
                    MapClientError(response.StatusCode),
                    $"RapidAPI request failed (HTTP {(int)response.StatusCode}).");
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                return ProviderResult.Failed(
                    ProviderErrorCode.InvalidProviderResponse,
                    "RapidAPI returned an empty response.");
            }

            return ParseSuccessPayload(body, mediaId, platform);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ProviderResult.Failed(
                ProviderErrorCode.ProviderTimeout,
                "RapidAPI request timed out.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "RapidAPI network error for media {MediaId}", mediaId);
            return ProviderResult.Failed(
                ProviderErrorCode.TemporaryFailure,
                "Network error while calling RapidAPI.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Unexpected RapidAPI failure for media {MediaId}", mediaId);
            return ProviderResult.Failed(
                ProviderErrorCode.TemporaryFailure,
                "Unexpected failure while resolving media with RapidAPI.");
        }
    }

    private ProviderResult ParseSuccessPayload(string body, Guid mediaId, MediaPlatform platform)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            // Some providers wrap payloads: { "data": { ... } } or { "result": { ... } }
            var payload = root;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryGetObjectProperty(root, "data", out var data))
                {
                    payload = data;
                }
                else if (TryGetObjectProperty(root, "result", out var result))
                {
                    payload = result;
                }
            }

            var downloadUrl = FirstString(payload, "download_url", "downloadUrl", "url", "video_url", "videoUrl");
            var caption = FirstString(payload, "caption", "title", "description", "text");
            var thumbUrl = ExtractThumbnailUrl(payload, downloadUrl);

            if (string.IsNullOrWhiteSpace(thumbUrl))
            {
                // Facebook payloads often nest picture/thumbnail objects or use alternate keys.
                thumbUrl = FindFirstImageUrl(payload, excludeUrl: downloadUrl);
            }

            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                _logger.LogWarning(
                    "RapidAPI missing download_url for media {MediaId} platform {Platform}. Body={Body}",
                    mediaId,
                    platform,
                    Truncate(body, 400));

                return ProviderResult.Failed(
                    ProviderErrorCode.MediaNotFound,
                    "RapidAPI did not return a downloadable video URL.");
            }

            if (!Uri.TryCreate(downloadUrl, UriKind.Absolute, out var mediaUri) ||
                (mediaUri.Scheme != Uri.UriSchemeHttps && mediaUri.Scheme != Uri.UriSchemeHttp))
            {
                return ProviderResult.Failed(
                    ProviderErrorCode.InvalidProviderResponse,
                    "RapidAPI returned a non-HTTP(S) download_url.");
            }

            if (string.IsNullOrWhiteSpace(thumbUrl))
            {
                _logger.LogWarning(
                    "RapidAPI missing thumbnail for media {MediaId} platform {Platform}",
                    mediaId,
                    platform);
            }

            return ProviderResult.Ok(
                resolvedSourceUrl: mediaUri.ToString(),
                title: string.IsNullOrWhiteSpace(caption) ? null : Truncate(caption.Trim(), 500),
                mimeType: "video/mp4",
                extension: ".mp4",
                thumbnailSourceUrl: thumbUrl);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "RapidAPI JSON parse failed for media {MediaId}", mediaId);
            return ProviderResult.Failed(
                ProviderErrorCode.InvalidProviderResponse,
                "RapidAPI returned invalid JSON.");
        }
    }

    private static string? ExtractThumbnailUrl(JsonElement payload, string? downloadUrl)
    {
        foreach (var name in ThumbnailPropertyNames)
        {
            var candidate = CoerceUrlFromProperty(payload, name);
            var normalized = NormalizeHttpUrl(candidate, excludeUrl: downloadUrl);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }
        }

        return null;
    }

    private static string? CoerceUrlFromProperty(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(name, out var prop))
        {
            return null;
        }

        return CoerceUrl(prop);
    }

    private static string? CoerceUrl(JsonElement prop)
    {
        switch (prop.ValueKind)
        {
            case JsonValueKind.String:
                return prop.GetString();
            case JsonValueKind.Object:
                return FirstString(prop, "url", "src", "uri", "href", "link", "secure_url", "secureUrl")
                    ?? CoerceUrlFromProperty(prop, "image")
                    ?? CoerceUrlFromProperty(prop, "thumbnail");
            case JsonValueKind.Array:
                foreach (var item in prop.EnumerateArray())
                {
                    var nested = CoerceUrl(item);
                    if (!string.IsNullOrWhiteSpace(nested))
                    {
                        return nested;
                    }
                }

                return null;
            default:
                return null;
        }
    }

    private static string? FindFirstImageUrl(JsonElement element, string? excludeUrl, int depth = 0)
    {
        if (depth > 6)
        {
            return null;
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return NormalizeHttpUrl(element.GetString(), excludeUrl, preferImage: true);
            case JsonValueKind.Object:
                foreach (var name in ThumbnailPropertyNames)
                {
                    if (!element.TryGetProperty(name, out var named))
                    {
                        continue;
                    }

                    var fromNamed = FindFirstImageUrl(named, excludeUrl, depth + 1);
                    if (!string.IsNullOrWhiteSpace(fromNamed))
                    {
                        return fromNamed;
                    }
                }

                foreach (var property in element.EnumerateObject())
                {
                    var nested = FindFirstImageUrl(property.Value, excludeUrl, depth + 1);
                    if (!string.IsNullOrWhiteSpace(nested))
                    {
                        return nested;
                    }
                }

                return null;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var nested = FindFirstImageUrl(item, excludeUrl, depth + 1);
                    if (!string.IsNullOrWhiteSpace(nested))
                    {
                        return nested;
                    }
                }

                return null;
            default:
                return null;
        }
    }

    private static string? NormalizeHttpUrl(string? raw, string? excludeUrl, bool preferImage = false)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var cleaned = UnescapeUrl(raw.Trim());
        if (!Uri.TryCreate(cleaned, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(excludeUrl) &&
            string.Equals(uri.ToString(), excludeUrl.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Never treat the MP4 download URL as a thumbnail.
        if (LooksLikeVideoUrl(uri))
        {
            return null;
        }

        if (preferImage && !LooksLikeImageUrl(uri) && !LooksLikeCdnThumbHost(uri))
        {
            return null;
        }

        return uri.ToString();
    }

    private static bool LooksLikeVideoUrl(Uri uri)
    {
        var path = uri.AbsolutePath;
        return path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".mov", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".webm", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeImageUrl(Uri uri)
    {
        var path = uri.AbsolutePath;
        return path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeCdnThumbHost(Uri uri)
    {
        var host = uri.Host;
        return host.Contains("fbcdn", StringComparison.OrdinalIgnoreCase)
            || host.Contains("cdninstagram", StringComparison.OrdinalIgnoreCase)
            || host.Contains("fbsbx", StringComparison.OrdinalIgnoreCase)
            || host.Contains("scontent", StringComparison.OrdinalIgnoreCase);
    }

    private static string UnescapeUrl(string value)
    {
        var unescaped = System.Net.WebUtility.HtmlDecode(value);
        return Regex.Replace(unescaped, @"\\/", "/");
    }

    private static ProviderErrorCode MapClientError(HttpStatusCode status) =>
        status switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => ProviderErrorCode.ConfigurationError,
            HttpStatusCode.NotFound => ProviderErrorCode.MediaNotFound,
            _ => ProviderErrorCode.TemporaryFailure,
        };

    private static bool TryGetObjectProperty(JsonElement root, string name, out JsonElement value)
    {
        if (root.TryGetProperty(name, out value) && value.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        value = default;
        return false;
    }

    private static string? FirstString(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var prop))
            {
                continue;
            }

            if (prop.ValueKind == JsonValueKind.String)
            {
                var s = prop.GetString();
                if (!string.IsNullOrWhiteSpace(s))
                {
                    return s;
                }
            }
        }

        return null;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
