namespace SocialReelSaver.Domain.Enums;

/// <summary>
/// Media processing status machine (SRS §12 / §13).
/// </summary>
public enum MediaStatus
{
    Preparing = 0,
    Queued = 1,
    Downloading = 2,
    Processing = 3,
    Completed = 4,
    Failed = 5,
}
