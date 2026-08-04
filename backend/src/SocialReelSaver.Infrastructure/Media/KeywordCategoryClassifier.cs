using System.Text.RegularExpressions;
using SocialReelSaver.Domain.Media;

namespace SocialReelSaver.Infrastructure.Media;

/// <summary>
/// Production keyword classifier. Pure/sync — no external APIs.
/// Highest-scoring category wins; ties prefer the first rule order (Games before Entertainment, etc.).
/// </summary>
public static class KeywordCategoryClassifier
{
    private static readonly (string Category, string[] Keywords)[] Rules =
    [
        ("Games",
        [
            "game", "gaming", "gamer", "pubg", "bgmi", "freefire", "free fire",
            "call of duty", "cod", "valorant", "minecraft", "roblox", "gta", "gta5",
            "fortnite", "steam", "playstation", "ps5", "xbox", "nintendo", "esports",
            "esport", "mobile legends", "league of legends", "dota",
        ]),
        ("Food & Dining",
        [
            "recipe", "cooking", "cook", "food", "restaurant", "chef", "baking", "meal",
            "cuisine", "street food", "dessert", "cafe", "kitchen",
        ]),
        ("Fashion & Clothing",
        [
            "fashion", "outfit", "clothing", "style", "wardrobe", "streetwear", "dress",
            "sneakers", "apparel", "ootd",
        ]),
        ("Fitness & Health",
        [
            "fitness", "workout", "gym", "health", "yoga", "cardio", "nutrition",
            "exercise", "wellness", "bodybuilding",
        ]),
        ("Travel",
        [
            "travel", "trip", "vacation", "tourism", "destination", "flight", "hotel",
            "backpacking", "wanderlust",
        ]),
        ("Technology",
        [
            "tech", "technology", "software", "hardware", "ai", "gadget", "smartphone",
            "coding", "programming", "startup tech", "laptop",
        ]),
        ("Education",
        [
            "education", "tutorial", "learn", "learning", "study", "course", "lesson",
            "school", "university", "howto", "how to",
        ]),
        ("Business",
        [
            "business", "entrepreneur", "startup", "marketing", "sales", "company",
            "agency", "branding",
        ]),
        ("Finance",
        [
            "finance", "investing", "investment", "stock", "crypto", "bitcoin", "money",
            "trading", "budget", "savings",
        ]),
        ("Motivation",
        [
            "motivation", "motivational", "inspire", "inspiration", "mindset", "success tips",
            "hustle",
        ]),
        ("Sports",
        [
            "sports", "football", "soccer", "cricket", "basketball", "tennis", "athletics",
            "nba", "fifa", "match highlight",
        ]),
        ("Beauty",
        [
            "beauty", "makeup", "skincare", "cosmetic", "haircare", "salon", "glowup",
        ]),
        ("Pets",
        [
            "pet", "pets", "dog", "cat", "puppy", "kitten", "animal rescue",
        ]),
        ("Automotive",
        [
            "car", "cars", "auto", "automotive", "vehicle", "motorcycle", "bike review",
            "ev car", "supercar", "hypercar", "drift", "bmw", "mercedes", "audi", "toyota",
            "honda", "ferrari", "lamborghini", "porsche", "mustang", "tesla", "racing",
            "offroad", "off-road", "suv", "pickup truck",
        ]),
        ("News",
        [
            "news", "breaking", "headline", "politics", "current affairs",
        ]),
        ("DIY & Crafts",
        [
            "diy", "craft", "crafts", "handmade", "woodworking", "sewing",
        ]),
        ("Photography",
        [
            "photography", "photographer", "camera", "lens", "portrait photo",
        ]),
        ("Music",
        [
            "music", "song", "singer", "rap", "album", "concert", "dj", "lyrics",
        ]),
        ("Comedy",
        [
            "comedy", "funny", "joke", "memes", "meme", "humor", "stand up",
        ]),
        ("Art & Design",
        [
            "art", "artist", "design", "illustration", "drawing", "painting", "graphic design",
        ]),
        ("Entertainment",
        [
            "movie", "film", "series", "netflix", "celebrity", "trailer", "tv show",
            "entertainment",
        ]),
        ("Lifestyle",
        [
            "lifestyle", "daily vlog", "vlog", "routine", "home decor", "self care",
        ]),
    ];

    public static string Classify(string? title, string? platform, string? originalUrl)
    {
        var haystack = string.Join(' ', title, platform, originalUrl).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(haystack))
        {
            return MediaCategories.Default;
        }

        string? bestCategory = null;
        var bestScore = 0;

        foreach (var (category, keywords) in Rules)
        {
            var score = 0;
            foreach (var keyword in keywords)
            {
                var k = keyword.Trim().ToLowerInvariant();
                if (k.Length == 0)
                {
                    continue;
                }

                if (k.Contains(' ', StringComparison.Ordinal))
                {
                    if (haystack.Contains(k, StringComparison.Ordinal))
                    {
                        score += 3;
                    }
                }
                else if (IsWordMatch(haystack, k))
                {
                    // Short tokens like "cod" / "ai" need word boundaries.
                    score += k.Length <= 3 ? 2 : 2;
                }
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestCategory = category;
            }
        }

        return bestScore > 0
            ? MediaCategories.NormalizeOrDefault(bestCategory)
            : MediaCategories.Default;
    }

    private static bool IsWordMatch(string haystack, string keyword)
    {
        // Word-boundary-ish match to reduce false positives (e.g. "ai" in "said").
        return Regex.IsMatch(
            haystack,
            $@"(^|[^\p{{L}}\p{{N}}]){Regex.Escape(keyword)}([^\p{{L}}\p{{N}}]|$)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
