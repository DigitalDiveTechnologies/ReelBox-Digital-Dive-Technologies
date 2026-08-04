using SocialReelSaver.Domain.Media;
using SocialReelSaver.Infrastructure.Media;
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
    [InlineData("", "General")]
    public void Classify_ReturnsExpectedCategory(string title, string expected)
    {
        var actual = KeywordCategoryClassifier.Classify(title, "Instagram", "https://instagram.com/reel/x");
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Normalize_MapsLegacyGamingToGames()
    {
        Assert.Equal("Games", MediaCategories.NormalizeOrDefault("Gaming"));
    }
}
