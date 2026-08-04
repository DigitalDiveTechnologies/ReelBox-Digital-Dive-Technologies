using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Application.Media.State;

/// <summary>
/// Enforces SRS §13 media state machine transitions.
/// </summary>
public static class MediaStateMachine
{
    private static readonly Dictionary<MediaStatus, HashSet<MediaStatus>> Allowed = new()
    {
        [MediaStatus.Preparing] = [MediaStatus.Queued, MediaStatus.Failed],
        [MediaStatus.Queued] = [MediaStatus.Downloading, MediaStatus.Failed],
        [MediaStatus.Downloading] = [MediaStatus.Processing, MediaStatus.Failed, MediaStatus.Queued],
        [MediaStatus.Processing] = [MediaStatus.Completed, MediaStatus.Failed, MediaStatus.Queued],
        [MediaStatus.Failed] = [MediaStatus.Queued],
        [MediaStatus.Completed] = [],
    };

    public static bool CanTransition(MediaStatus from, MediaStatus to) =>
        Allowed.TryGetValue(from, out var next) && next.Contains(to);

    public static void EnsureTransition(MediaStatus from, MediaStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidOperationException(
                $"Invalid media status transition: {from} -> {to}.");
        }
    }
}
