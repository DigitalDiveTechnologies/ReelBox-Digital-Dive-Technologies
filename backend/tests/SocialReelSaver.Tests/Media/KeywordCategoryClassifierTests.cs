using SocialReelSaver.Domain.Entities;
using SocialReelSaver.Domain.Enums;
using SocialReelSaver.Domain.Media;
using SocialReelSaver.Infrastructure.Media;
using SocialReelSaver.Infrastructure.Media.Categorization;
using Xunit;

namespace SocialReelSaver.Tests.Media;

public sealed class KeywordCategoryClassifierTests
{
    [Theory]
    [InlineData("PUBG mobile highlights", "Games")]
    [InlineData("Free Fire ranked match", "Games")]
    [InlineData("Call of Duty warzone tips", "Games")]
    [InlineData("valorant aim training", "Games")]
    [InlineData("Best pasta recipe ever", "Food & Dining")]
    [InlineData("Morning gym workout", "Fitness & Health")]
    [InlineData("BMW M4 drift car review", "Automotive")]
    [InlineData("street food tour in Bangkok #foodie", "Food & Dining")]
    [InlineData("web development crash course", "Technology")]
    [InlineData("machine learning basics", "Technology")]
    [InlineData("road trip through mountains", "Travel")]
    [InlineData("gta v online heist", "Games")]
    [InlineData("", "General")]
    public void Classify_ReturnsExpectedCategory(string title, string expected)
    {
        var actual = KeywordCategoryClassifier.Classify(title, "Instagram", "https://instagram.com/reel/x");
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WeightedEngine_UsesDescriptionHashtagsAndUploader()
    {
        var item = new MediaItem
        {
            Title = "Weekend vibes",
            Description = "Ranked grind tonight #pubgmobile #gaming",
            CreatorUsername = "pro_gamer_clips",
            OriginalUrl = "https://www.instagram.com/reel/abc123/",
            Platform = MediaPlatform.Instagram,
            MetadataText = "pubg mobile esports",
        };

        var result = WeightedKeywordCategoryEngine.Classify(CategorizationSignals.FromMediaItem(item));
        Assert.Equal("Games", result.Category);
        Assert.Equal(ClassificationSources.KeywordEngine, result.Source);
        Assert.True(result.RawScore >= WeightedKeywordCategoryEngine.MinimumScoreThreshold);
        Assert.True(result.Confidence >= 0.40);
    }

    [Fact]
    public void WeightedEngine_FallsBackToGeneral_WhenBelowThreshold()
    {
        var item = new MediaItem
        {
            Title = "hello world",
            OriginalUrl = "https://www.instagram.com/reel/xyz/",
            Platform = MediaPlatform.Instagram,
        };

        var result = WeightedKeywordCategoryEngine.Classify(CategorizationSignals.FromMediaItem(item));
        Assert.Equal(MediaCategories.Default, result.Category);
    }

    [Fact]
    public void TextNormalizer_SplitsCamelSnakeKebabAndHashtags()
    {
        Assert.Equal("machine learning", TextNormalizer.Normalize("machineLearning"));
        Assert.Equal("web development", TextNormalizer.Normalize("web_development"));
        Assert.Equal("car review", TextNormalizer.Normalize("car-review"));
        Assert.Contains("street food", TextNormalizer.ExtractHashtags("#StreetFood #Travel"));
    }

    [Fact]
    public void Normalize_MapsLegacyGamingToGames()
    {
        Assert.Equal("Games", MediaCategories.NormalizeOrDefault("Gaming"));
    }
}
