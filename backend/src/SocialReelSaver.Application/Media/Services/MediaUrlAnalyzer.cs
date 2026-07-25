using System.Text.RegularExpressions;
using SocialReelSaver.Application.Abstractions.Media;
using SocialReelSaver.Application.Common.Exceptions;
using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Application.Media.Services;

public sealed class MediaUrlAnalyzer : IMediaUrlAnalyzer
{
    private static readonly HashSet<string> InstagramHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "instagram.com",
        "www.instagram.com",
        "m.instagram.com",
    };

    private static readonly HashSet<string> FacebookHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "facebook.com",
        "www.facebook.com",
        "m.facebook.com",
        "fb.com",
        "www.fb.com",
        "fb.watch",
        "www.fb.watch",
    };

    public MediaUrlAnalysis Analyze(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new BadRequestException("URL is required.", "INVALID_URL");
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            throw new BadRequestException(
                "URL must be an absolute http or https link.",
                "INVALID_URL");
        }

        if (string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new BadRequestException("URL host is missing.", "INVALID_URL");
        }

        var platform = DetectPlatform(uri.Host);
        if (platform is null)
        {
            throw new BadRequestException(
                "Unsupported platform. Only Instagram and Facebook URLs are accepted.",
                "UNSUPPORTED_PLATFORM");
        }

        var normalized = Normalize(uri);
        return new MediaUrlAnalysis(uri.ToString(), normalized, platform.Value);
    }

    private static MediaPlatform? DetectPlatform(string host)
    {
        if (InstagramHosts.Contains(host) || host.EndsWith(".instagram.com", StringComparison.OrdinalIgnoreCase))
        {
            return MediaPlatform.Instagram;
        }

        if (FacebookHosts.Contains(host)
            || host.EndsWith(".facebook.com", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".fb.com", StringComparison.OrdinalIgnoreCase))
        {
            return MediaPlatform.Facebook;
        }

        return null;
    }

    private static string Normalize(Uri uri)
    {
        var builder = new UriBuilder(uri)
        {
            Scheme = uri.Scheme.ToLowerInvariant(),
            Host = uri.Host.ToLowerInvariant(),
            Port = -1,
            Fragment = string.Empty,
        };

        if (builder.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            builder.Host = builder.Host[4..];
        }

        var path = builder.Path.TrimEnd('/');
        builder.Path = string.IsNullOrEmpty(path) ? "/" : path;

        // Drop common tracking parameters while preserving content identifiers.
        var query = RemoveTrackingParams(uri.Query);
        builder.Query = query;

        return builder.Uri.ToString().TrimEnd('/');
    }

    private static string RemoveTrackingParams(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return string.Empty;
        }

        var pairs = query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(p =>
            {
                var key = p.Split('=', 2)[0];
                return !Regex.IsMatch(key, @"^(utm_|fbclid|igshid|ref)", RegexOptions.IgnoreCase);
            })
            .ToArray();

        return pairs.Length == 0 ? string.Empty : string.Join('&', pairs);
    }
}
