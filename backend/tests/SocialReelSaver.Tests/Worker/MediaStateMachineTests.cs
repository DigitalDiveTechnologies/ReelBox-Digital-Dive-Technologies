using SocialReelSaver.Application.Media.State;
using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Tests.Worker;

public sealed class MediaStateMachineTests
{
    [Theory]
    [InlineData(MediaStatus.Preparing, MediaStatus.Queued, true)]
    [InlineData(MediaStatus.Queued, MediaStatus.Downloading, true)]
    [InlineData(MediaStatus.Downloading, MediaStatus.Processing, true)]
    [InlineData(MediaStatus.Processing, MediaStatus.Completed, true)]
    [InlineData(MediaStatus.Downloading, MediaStatus.Failed, true)]
    [InlineData(MediaStatus.Processing, MediaStatus.Failed, true)]
    [InlineData(MediaStatus.Failed, MediaStatus.Queued, true)]
    [InlineData(MediaStatus.Completed, MediaStatus.Failed, false)]
    [InlineData(MediaStatus.Queued, MediaStatus.Completed, false)]
    [InlineData(MediaStatus.Preparing, MediaStatus.Completed, false)]
    [InlineData(MediaStatus.Downloading, MediaStatus.Completed, false)]
    public void CanTransition_EnforcesSrsRules(MediaStatus from, MediaStatus to, bool expected)
    {
        Assert.Equal(expected, MediaStateMachine.CanTransition(from, to));
    }

    [Fact]
    public void EnsureTransition_Invalid_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            MediaStateMachine.EnsureTransition(MediaStatus.Completed, MediaStatus.Queued));
    }
}
