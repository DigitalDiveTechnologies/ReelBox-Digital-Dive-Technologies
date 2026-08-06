using SocialReelSaver.Domain.Entities;

namespace SocialReelSaver.Infrastructure.Media.Categorization;

/// <summary>
/// Weighted input channels for short-form reel classification (offline keyword engine).
/// </summary>
public sealed record CategorizationSignals(
    string Title,
    string Description,
    string Caption,
    string Hashtags,
    string UrlTokens,
    string Filename,
    string Username,
    string Platform,
    string Tags,
    string AudioTitle,
    string ExtraMetadata)
{
    // Field base weights (applied per match).
    public const int WeightDescription = 5;
    public const int WeightCaption = 5;
    public const int WeightHashtags = 4;
    public const int WeightTitle = 3;
    public const int WeightTags = 3;
    public const int WeightUrlTokens = 2;
    public const int WeightFilename = 2;
    public const int WeightExtraMetadata = 2;
    public const int WeightUsername = 1;
    public const int WeightPlatform = 1;
    public const int WeightAudioTitle = 1;

    /// <summary>Bonus when the same phrase hits in 2+ distinct fields.</summary>
    public const int MultiFieldBonus = 3;

    public static CategorizationSignals FromMediaItem(MediaItem item)
    {
        var titleRaw = item.Title;
        var descriptionRaw = item.Description;
        var hashtagSource = string.Join(' ', titleRaw, descriptionRaw, item.MetadataText);
        var hashtags = string.Join(' ', TextNormalizer.ExtractHashtags(hashtagSource));

        string titleText;
        string descriptionText;
        string captionText;

        if (!string.IsNullOrWhiteSpace(descriptionRaw))
        {
            descriptionText = TextNormalizer.StripHashtags(descriptionRaw);
            captionText = descriptionText;
            titleText = TextNormalizer.StripHashtags(titleRaw);
        }
        else
        {
            // Short-form: title often IS the caption — score full text once as Caption,
            // keep a shorter Title channel to avoid double-counting the same string.
            captionText = TextNormalizer.StripHashtags(titleRaw);
            descriptionText = string.Empty;
            titleText = ShortHeadline(captionText);
        }

        var urlTokens = TextNormalizer.JoinNormalized(
            string.Join(' ', TextNormalizer.TokenizeUrl(item.OriginalUrl)),
            string.Join(' ', TextNormalizer.TokenizeUrl(item.NormalizedUrl)));

        var filename = TextNormalizer.Normalize(
            string.Join(
                ' ',
                TextNormalizer.TokenizeUrl(item.MediaStorageKey),
                TextNormalizer.TokenizeUrl(item.ThumbnailStorageKey),
                item.MetadataText));

        var user = !string.IsNullOrWhiteSpace(item.CreatorUsername)
            ? TextNormalizer.Normalize(item.CreatorUsername)
            : TextNormalizer.ExtractUsernameFromUrl(item.OriginalUrl) ?? string.Empty;

        var platform = TextNormalizer.Normalize(item.Platform.ToString());
        var tags = TextNormalizer.Normalize(item.MetadataText);
        var extra = TextNormalizer.JoinNormalized(item.MimeType, item.MetadataText);
        var audio = string.Empty;

        return new CategorizationSignals(
            Title: titleText,
            Description: descriptionText,
            Caption: captionText,
            Hashtags: hashtags,
            UrlTokens: urlTokens,
            Filename: filename,
            Username: user,
            Platform: platform,
            Tags: tags,
            AudioTitle: audio,
            ExtraMetadata: extra);
    }

    private static string ShortHeadline(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var firstLine = text.Split(['\n', '.', '!', '?'], 2, StringSplitOptions.TrimEntries)[0];
        return firstLine.Length <= 72 ? firstLine : firstLine[..72];
    }

    public IEnumerable<(string Field, int Weight)> WeightedFields()
    {
        yield return (Description, WeightDescription);
        yield return (Caption, WeightCaption);
        yield return (Hashtags, WeightHashtags);
        yield return (Title, WeightTitle);
        yield return (Tags, WeightTags);
        yield return (UrlTokens, WeightUrlTokens);
        yield return (Filename, WeightFilename);
        yield return (ExtraMetadata, WeightExtraMetadata);
        yield return (Username, WeightUsername);
        yield return (Platform, WeightPlatform);
        yield return (AudioTitle, WeightAudioTitle);
    }
}

public sealed record CategoryScoreResult(
    string Category,
    double Confidence,
    int RawScore,
    string Source);
