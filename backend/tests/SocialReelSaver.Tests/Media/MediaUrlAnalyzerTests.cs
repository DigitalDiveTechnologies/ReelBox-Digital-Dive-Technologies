using SocialReelSaver.Application.Common.Exceptions;
using SocialReelSaver.Application.Media.Services;
using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Tests.Media;

public sealed class MediaUrlAnalyzerTests
{
    private readonly MediaUrlAnalyzer _sut = new();

    [Theory]
    [InlineData("https://www.instagram.com/reel/ABC123/", MediaPlatform.Instagram)]
    [InlineData("https://instagram.com/p/XYZ/", MediaPlatform.Instagram)]
    [InlineData("https://www.facebook.com/watch/?v=123", MediaPlatform.Facebook)]
    [InlineData("https://fb.watch/abc/", MediaPlatform.Facebook)]
    public void Analyze_SupportedUrls_DetectsPlatform(string url, MediaPlatform expected)
    {
        var result = _sut.Analyze(url);

        Assert.Equal(expected, result.Platform);
        Assert.False(string.IsNullOrWhiteSpace(result.NormalizedUrl));
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://instagram.com/reel/1")]
    public void Analyze_InvalidUrl_Throws(string url)
    {
        var ex = Assert.Throws<BadRequestException>(() => _sut.Analyze(url));
        Assert.Equal("INVALID_URL", ex.Code);
    }

    [Fact]
    public void Analyze_UnsupportedDomain_Throws()
    {
        var ex = Assert.Throws<BadRequestException>(
            () => _sut.Analyze("https://tiktok.com/@user/video/1"));
        Assert.Equal("UNSUPPORTED_PLATFORM", ex.Code);
    }

    [Fact]
    public void Analyze_NormalizesHostAndTracking()
    {
        var result = _sut.Analyze("https://WWW.Instagram.com/reel/ABC123/?utm_source=share&igshid=xyz");

        Assert.Equal(MediaPlatform.Instagram, result.Platform);
        Assert.DoesNotContain("utm_source", result.NormalizedUrl);
        Assert.DoesNotContain("www.", result.NormalizedUrl);
    }
}
