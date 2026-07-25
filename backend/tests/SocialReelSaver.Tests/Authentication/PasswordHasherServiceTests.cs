using SocialReelSaver.Infrastructure.Authentication;

namespace SocialReelSaver.Tests.Authentication;

public sealed class PasswordHasherServiceTests
{
    private readonly PasswordHasherService _sut = new();

    [Fact]
    public void HashPassword_ThenVerify_Succeeds()
    {
        var hash = _sut.HashPassword("SecurePass1");

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.True(_sut.VerifyPassword("SecurePass1", hash));
        Assert.False(_sut.VerifyPassword("WrongPass1", hash));
    }
}
