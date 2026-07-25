using Microsoft.Extensions.Options;
using SocialReelSaver.Infrastructure.Authentication;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Tests.Authentication;

public sealed class JwtTokenServiceTests
{
    private readonly JwtTokenService _sut = new(Options.Create(new JwtOptions
    {
        Issuer = "SocialReelSaver",
        Audience = "SocialReelSaver.Mobile",
        SigningKey = "TEST_ONLY_SIGNING_KEY_AT_LEAST_32_CHARS_LONG!!",
        AccessTokenExpirationMinutes = 15,
        RefreshTokenExpirationDays = 7,
    }));

    [Fact]
    public async Task CreateAccessTokenAsync_ReturnsJwt()
    {
        var token = await _sut.CreateAccessTokenAsync(Guid.NewGuid(), "user@example.com");

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal(2, token.Count(c => c == '.'));
    }

    [Fact]
    public async Task CreateRefreshTokenAsync_ReturnsOpaqueToken()
    {
        var token = await _sut.CreateRefreshTokenAsync();

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.DoesNotContain('.', token);
    }
}
