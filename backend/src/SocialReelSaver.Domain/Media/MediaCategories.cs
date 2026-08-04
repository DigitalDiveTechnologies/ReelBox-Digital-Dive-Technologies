namespace SocialReelSaver.Domain.Media;

/// <summary>
/// Predefined reel categories for AI classification (single label per item).
/// </summary>
public static class MediaCategories
{
    public const string Default = "General";

    public static readonly IReadOnlyList<string> All =
    [
        "General",
        "Food & Dining",
        "Fashion & Clothing",
        "Fitness & Health",
        "Travel",
        "Technology",
        "Education",
        "Business",
        "Finance",
        "Motivation",
        "Entertainment",
        "Sports",
        "Gaming",
        "Beauty",
        "Pets",
        "Automotive",
        "News",
        "Lifestyle",
        "DIY & Crafts",
        "Photography",
        "Music",
        "Comedy",
        "Art & Design",
        "Other",
    ];

    public static string NormalizeOrDefault(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Default;
        }

        var cleaned = raw.Trim()
            .Trim('"', '\'', '`')
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty)
            .Trim();

        foreach (var category in All)
        {
            if (string.Equals(category, cleaned, StringComparison.OrdinalIgnoreCase))
            {
                return category;
            }
        }

        // Soft match: short labels like "Food" → "Food & Dining"
        foreach (var category in All)
        {
            var firstToken = category.Split([' ', '&'], StringSplitOptions.RemoveEmptyEntries)[0];
            if (string.Equals(firstToken, cleaned, StringComparison.OrdinalIgnoreCase))
            {
                return category;
            }
        }

        return Default;
    }
}
