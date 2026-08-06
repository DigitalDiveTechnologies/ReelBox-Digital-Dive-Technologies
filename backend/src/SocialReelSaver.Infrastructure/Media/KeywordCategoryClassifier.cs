using SocialReelSaver.Domain.Entities;
using SocialReelSaver.Infrastructure.Media.Categorization;

namespace SocialReelSaver.Infrastructure.Media;

/// <summary>
/// Backward-compatible facade over <see cref="WeightedKeywordCategoryEngine"/>.
/// </summary>
public static class KeywordCategoryClassifier
{
    public static string Classify(string? title, string? platform, string? originalUrl)
    {
        var item = new MediaItem
        {
            Title = title,
            OriginalUrl = originalUrl ?? string.Empty,
            Platform = Enum.TryParse<Domain.Enums.MediaPlatform>(platform, true, out var p)
                ? p
                : Domain.Enums.MediaPlatform.Instagram,
        };
        return WeightedKeywordCategoryEngine.Classify(CategorizationSignals.FromMediaItem(item)).Category;
    }
}
