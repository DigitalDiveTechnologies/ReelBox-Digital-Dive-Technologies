using Microsoft.Extensions.Options;
using SocialReelSaver.Application.Media.Retry;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Tests.Worker;

public sealed class ExponentialBackoffRetryPolicyTests
{
    private readonly ExponentialBackoffRetryPolicy _sut = new(Options.Create(new WorkerOptions
    {
        MaxRetries = 3,
        BaseBackoffSeconds = 2,
        MaxBackoffSeconds = 30,
    }));

    [Theory]
    [InlineData("PROVIDER_TEMPORARY_FAILURE", true)]
    [InlineData("DOWNLOAD_TIMEOUT", true)]
    [InlineData("STORAGE_FAILURE", true)]
    [InlineData("UNKNOWN", true)]
    [InlineData("FILE_TOO_LARGE", false)]
    [InlineData("ACCESS_NOT_PERMITTED", false)]
    [InlineData("PROVIDER_NOT_IMPLEMENTED", false)]
    public void IsRetryable_MatchesSrsCategories(string code, bool expected)
    {
        Assert.Equal(expected, _sut.IsRetryable(code));
    }

    [Fact]
    public void GetBackoffDelay_GrowsExponentiallyAndCaps()
    {
        Assert.Equal(TimeSpan.FromSeconds(2), _sut.GetBackoffDelay(1));
        Assert.Equal(TimeSpan.FromSeconds(4), _sut.GetBackoffDelay(2));
        Assert.Equal(TimeSpan.FromSeconds(8), _sut.GetBackoffDelay(3));
        Assert.Equal(TimeSpan.FromSeconds(16), _sut.GetBackoffDelay(4));
        Assert.Equal(TimeSpan.FromSeconds(30), _sut.GetBackoffDelay(5));
    }

    [Fact]
    public void CanRetry_RespectsMaxRetries()
    {
        Assert.True(_sut.CanRetry(0, "PROVIDER_TEMPORARY_FAILURE"));
        Assert.True(_sut.CanRetry(2, "PROVIDER_TEMPORARY_FAILURE"));
        Assert.False(_sut.CanRetry(3, "PROVIDER_TEMPORARY_FAILURE"));
    }
}
