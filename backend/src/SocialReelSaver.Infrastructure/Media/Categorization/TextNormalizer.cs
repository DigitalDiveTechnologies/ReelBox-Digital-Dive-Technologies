using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SocialReelSaver.Infrastructure.Media.Categorization;

/// <summary>
/// Deterministic normalizer for short-form social captions, hashtags, and URLs.
/// </summary>
public static partial class TextNormalizer
{
    private static readonly Regex EmojiRegex = CreateEmojiRegex();
    private static readonly Regex NonAlnumRegex = CreateNonAlnumRegex();
    private static readonly Regex CamelBoundaryRegex = CreateCamelBoundaryRegex();
    private static readonly Regex MultiSpaceRegex = CreateMultiSpaceRegex();
    private static readonly Regex HashtagRegex = CreateHashtagRegex();

    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var text = raw.Normalize(NormalizationForm.FormKC);
        text = EmojiRegex.Replace(text, " ");
        text = text.Replace('#', ' ').Replace('@', ' ');
        text = text.Replace('_', ' ').Replace('-', ' ');
        text = CamelBoundaryRegex.Replace(text, "$1 $2");
        text = text.ToLowerInvariant();
        text = NonAlnumRegex.Replace(text, " ");
        text = MultiSpaceRegex.Replace(text, " ").Trim();
        return text;
    }

    public static IReadOnlyList<string> ExtractHashtags(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        var tags = new List<string>();
        foreach (Match match in HashtagRegex.Matches(raw))
        {
            var tag = Normalize(match.Groups[1].Value);
            if (tag.Length > 0)
            {
                tags.Add(tag);
            }
        }

        return tags;
    }

    public static string StripHashtags(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        return Normalize(HashtagRegex.Replace(raw, " "));
    }

    public static IReadOnlyList<string> TokenizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return [];
        }

        string decoded;
        try
        {
            decoded = Uri.UnescapeDataString(url);
        }
        catch (UriFormatException)
        {
            decoded = url;
        }

        var normalized = Normalize(decoded.Replace('/', ' ').Replace('.', ' ').Replace('?', ' ').Replace('=', ' ').Replace('&', ' '));
        if (normalized.Length == 0)
        {
            return [];
        }

        return normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public static string? ExtractUsernameFromUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return null;
        }

        // instagram.com/{user}/reel/...  or facebook.com/{user}/videos/...
        var candidate = segments[0];
        if (candidate is "reel" or "reels" or "p" or "tv" or "watch" or "share" or "videos" or "video" or "posts")
        {
            return null;
        }

        return Normalize(candidate);
    }

    public static string JoinNormalized(params string?[] parts)
    {
        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            var n = Normalize(part);
            if (n.Length == 0)
            {
                continue;
            }

            if (sb.Length > 0)
            {
                sb.Append(' ');
            }

            sb.Append(n);
        }

        return sb.ToString();
    }

    [GeneratedRegex(@"\p{Cs}|\p{So}|[\u2600-\u27BF]|[\uFE00-\uFE0F]|[\u200D]", RegexOptions.CultureInvariant)]
    private static partial Regex CreateEmojiRegex();

    [GeneratedRegex(@"[^\p{L}\p{N}\s]+", RegexOptions.CultureInvariant)]
    private static partial Regex CreateNonAlnumRegex();

    [GeneratedRegex(@"(\p{Ll})(\p{Lu})", RegexOptions.CultureInvariant)]
    private static partial Regex CreateCamelBoundaryRegex();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex CreateMultiSpaceRegex();

    [GeneratedRegex(@"#([\p{L}\p{N}_]+)", RegexOptions.CultureInvariant)]
    private static partial Regex CreateHashtagRegex();
}
