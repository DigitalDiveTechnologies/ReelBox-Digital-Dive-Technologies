using System.Text.RegularExpressions;
using SocialReelSaver.Domain.Media;

namespace SocialReelSaver.Infrastructure.Media.Categorization;

/// <summary>
/// Offline, deterministic weighted keyword/phrase engine for short-form social metadata.
/// </summary>
public static class WeightedKeywordCategoryEngine
{
    /// <summary>Minimum raw score required to assign a non-General category.</summary>
    public const int MinimumScoreThreshold = 4;

    private static readonly (string Category, string[] Phrases, int Strength)[] Lexicon =
    [
        ("Games",
        [
            "pubg mobile", "call of duty", "league of legends", "mobile legends", "gta v", "gta 5", "gta5",
            "free fire", "god of war", "elden ring", "apex legends", "counter strike", "cs go", "cs2",
            "gaming", "gamer", "gameplay", "esports", "esport", "pubg", "bgmi", "freefire", "valorant",
            "minecraft", "roblox", "fortnite", "steam", "playstation", "ps5", "xbox", "nintendo", "dota",
            "cod", "warzone", "streamer", "twitch", "game", "games", "rpg", "fps game", "mobile game",
        ], 3),
        ("Food & Dining",
        [
            "street food", "food review", "home cooking", "meal prep", "foodie", "mukbang", "food porn",
            "recipe", "recipes", "cooking", "cook", "cooked", "food", "foods", "restaurant", "restaurants",
            "chef", "baking", "bake", "baked", "meal", "meals", "cuisine", "dessert", "desserts", "cafe",
            "kitchen", "brunch", "bbq", "grill", "pizza", "burger", "pasta", "sushi", "noodles", "snack",
        ], 2),
        ("Fashion & Clothing",
        [
            "street wear", "streetwear", "outfit of the day", "fashion haul", "fashion", "outfit", "outfits",
            "clothing", "clothes", "style", "wardrobe", "dress", "dresses", "sneakers", "apparel", "ootd",
            "runway", "couture", "lookbook", "thrift", "fashionista", "model walk",
        ], 2),
        ("Fitness & Health",
        [
            "gym motivation", "home workout", "weight loss", "weight training", "fitness", "workout", "workouts",
            "gym", "health", "healthy", "yoga", "cardio", "nutrition", "exercise", "exercises", "wellness",
            "bodybuilding", "crossfit", "hiit", "calisthenics", "protein", "running", "mental health",
            "meditation", "stretching", "abs workout",
        ], 2),
        ("Travel",
        [
            "road trip", "travel vlog", "travel guide", "travel", "travels", "traveling", "travelling",
            "trip", "trips", "vacation", "vacations", "tourism", "tourist", "destination", "destinations",
            "flight", "hotel", "hotels", "backpacking", "wanderlust", "itinerary", "airport", "sightseeing",
            "solo travel", "nature travel", "hiking trail", "national park",
        ], 2),
        ("Technology",
        [
            "web development", "machine learning", "artificial intelligence", "software engineering",
            "app development", "tech review", "gadget review", "tech", "technology", "technologies",
            "software", "hardware",             "ai", "gadget", "gadgets", "smartphone", "smartphones", "coding",
            "programming", "programmer", "startup tech", "laptop", "laptops", "iphone", "android",
            "devops", "cybersecurity", "chatgpt", "llm", "robotics", "crash course",
            "nasa", "physics experiment", "chemistry lab", "basics tutorial tech",
        ], 3),
        ("Education",
        [
            "how to", "howto", "study tips", "education", "educational", "tutorial", "tutorials",
            "learn", "learning", "study", "studying", "course", "courses", "lesson", "lessons",
            "school", "university", "exam", "exams", "lecture", "lectures", "teacher", "science",
            "biology", "chemistry", "physics", "math", "mathematics", "history lesson",
        ], 2),
        ("Business",
        [
            "business tips", "business", "businesses", "entrepreneur", "entrepreneurs", "startup", "startups",
            "marketing", "sales", "company", "companies", "agency", "branding", "ecommerce", "e commerce",
            "saas", "b2b", "side hustle", "small business",
        ], 2),
        ("Finance",
        [
            "personal finance", "stock market", "finance", "financial", "investing", "investment", "investments",
            "stock", "stocks", "crypto", "cryptocurrency", "bitcoin", "money", "trading", "trader", "budget",
            "savings", "forex", "nft", "ethereum", "wealth",
        ], 2),
        ("Motivation",
        [
            "success tips", "gym motivation", "motivation", "motivational", "inspire", "inspiration",
            "inspired", "mindset", "hustle", "discipline", "grind", "self improvement", "self help",
        ], 2),
        ("Sports",
        [
            "match highlight", "sports", "sport", "football", "soccer", "cricket", "basketball", "tennis",
            "athletics", "athlete", "nba", "fifa", "ufc", "boxing", "highlight", "highlights",
            "premier league", "ipl", "olympics", "workout sports",
        ], 2),
        ("Beauty",
        [
            "make up", "skin care", "hair care", "beauty", "makeup", "skincare", "cosmetic", "cosmetics",
            "haircare", "salon", "glowup", "glow up", "foundation", "lipstick", "manicure",
        ], 2),
        ("Pets",
        [
            "animal rescue", "pet", "pets", "dog", "dogs", "cat", "cats", "puppy", "puppies", "kitten",
            "kittens", "rescue animal", "doggo", "kitty", "wildlife", "bird", "birds", "aquarium",
        ], 2),
        ("Automotive",
        [
            "car review", "bike review", "test drive", "pick up truck", "pickup truck", "off road",
            "car", "cars", "auto", "automotive", "vehicle", "vehicles", "motorcycle", "motorcycles",
            "ev car", "supercar", "hypercar", "drift", "bmw", "mercedes", "audi", "toyota", "honda",
            "ferrari", "lamborghini", "porsche", "mustang", "tesla", "racing", "race car", "offroad",
            "suv", "tuning", "exhaust", "jdm", "mechanic",
        ], 3),
        ("News",
        [
            "breaking news", "current affairs", "news", "breaking", "headline", "headlines", "politics",
            "political", "world news", "journalism",
        ], 2),
        ("DIY & Crafts",
        [
            "do it yourself", "wood working", "home diy", "home improvement", "diy", "craft", "crafts",
            "handmade", "woodworking", "sewing", "maker", "3d print", "3d printing", "home decor diy",
            "furniture makeover", "renovation",
        ], 2),
        ("Photography",
        [
            "portrait photo", "photography", "photographer", "photographers", "camera", "cameras", "lens",
            "lenses", "cinematography", "dslr", "mirrorless", "photo tip", "photo tips", "videography",
        ], 2),
        ("Music",
        [
            "music video", "music", "song", "songs", "singer", "singers", "rap", "rapper", "album", "albums",
            "concert", "concerts", "dj", "lyrics", "beat", "beats", "cover song", "spotify", "playlist",
        ], 2),
        ("Comedy",
        [
            "stand up", "stand up comedy", "comedy", "funny", "joke", "jokes", "memes", "meme", "humor",
            "humour", "skit", "skits", "prank", "pranks",
        ], 2),
        ("Art & Design",
        [
            "graphic design", "digital art", "art", "arts", "artist", "artists", "design", "designs",
            "illustration", "drawing", "drawings", "painting", "paintings", "ui ux", "figma", "sketch",
        ], 2),
        ("Entertainment",
        [
            "tv show", "tv shows", "movie trailer", "movie", "movies", "film", "films", "series", "netflix",
            "celebrity", "celebrities", "trailer", "trailers", "entertainment", "bollywood", "hollywood",
            "anime", "kdrama", "cinema", "actor", "actress", "binge watch",
        ], 2),
        ("Lifestyle",
        [
            "daily vlog", "self care", "home decor", "lifestyle", "vlog", "vlogs", "routine", "routines",
            "day in my life", "minimalism", "grwm", "home tips", "interior design", "nature walk",
            "garden", "gardening", "plants", "plant care",
        ], 2),
    ];

    public static CategoryScoreResult Classify(CategorizationSignals signals)
    {
        string? bestCategory = null;
        var bestScore = 0;
        var secondScore = 0;

        foreach (var (category, phrases, strength) in Lexicon)
        {
            var score = ScoreCategory(signals, phrases, strength);
            if (score > bestScore)
            {
                secondScore = bestScore;
                bestScore = score;
                bestCategory = category;
            }
            else if (score > secondScore)
            {
                secondScore = score;
            }
        }

        if (bestScore < MinimumScoreThreshold || bestCategory is null)
        {
            return new CategoryScoreResult(
                MediaCategories.Default,
                Confidence: 0.2,
                RawScore: bestScore,
                ClassificationSources.KeywordEngine);
        }

        var margin = bestScore - secondScore;
        var confidence = Math.Clamp(
            0.40 + (bestScore / 50.0) + (margin / 25.0),
            0.40,
            0.98);

        return new CategoryScoreResult(
            MediaCategories.NormalizeOrDefault(bestCategory),
            confidence,
            bestScore,
            ClassificationSources.KeywordEngine);
    }

    private static int ScoreCategory(
        CategorizationSignals signals,
        string[] phrases,
        int categoryStrength)
    {
        var score = 0;
        foreach (var phrase in phrases)
        {
            var normalizedPhrase = ExpandVariants(phrase);
            foreach (var variant in normalizedPhrase)
            {
                var hitFields = 0;
                var phraseScore = 0;
                foreach (var (field, weight) in signals.WeightedFields())
                {
                    if (string.IsNullOrWhiteSpace(field))
                    {
                        continue;
                    }

                    if (!Matches(field, variant))
                    {
                        continue;
                    }

                    hitFields++;
                    var strongBoost = variant.Contains(' ', StringComparison.Ordinal) ? 2 : 0;
                    phraseScore += weight + strongBoost + Math.Max(0, categoryStrength - 2);
                }

                if (hitFields == 0)
                {
                    continue;
                }

                if (hitFields >= 2)
                {
                    phraseScore += CategorizationSignals.MultiFieldBonus * (hitFields - 1);
                }

                score += phraseScore;
                // Count strongest variant only for this base phrase.
                break;
            }
        }

        return score;
    }

    private static IEnumerable<string> ExpandVariants(string phrase)
    {
        var p = phrase.Trim().ToLowerInvariant();
        if (p.Length == 0)
        {
            yield break;
        }

        yield return p;

        // Simple plural / singular variants for single tokens (no -ing stems).
        if (!p.Contains(' ', StringComparison.Ordinal))
        {
            if (p.EndsWith("ies", StringComparison.Ordinal) && p.Length > 4)
            {
                yield return p[..^3] + "y";
            }
            else if (p.EndsWith('s') && p.Length > 3 && !p.EndsWith("ss", StringComparison.Ordinal))
            {
                yield return p[..^1];
            }
            else
            {
                yield return p + "s";
            }
        }
    }

    private static bool Matches(string fieldText, string phrase)
    {
        if (phrase.Contains(' ', StringComparison.Ordinal))
        {
            return fieldText.Contains(phrase, StringComparison.Ordinal);
        }

        return IsWordMatch(fieldText, phrase);
    }

    private static bool IsWordMatch(string haystack, string keyword) =>
        Regex.IsMatch(
            haystack,
            $@"(^|[^\p{{L}}\p{{N}}]){Regex.Escape(keyword)}([^\p{{L}}\p{{N}}]|$)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
}
