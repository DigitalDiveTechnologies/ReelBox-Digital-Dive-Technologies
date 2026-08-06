namespace SocialReelSaver.Application.Abstractions.Media;

/// <summary>
/// Pluggable media categorizer (metadata only). Never blocks download.
/// Default: offline weighted keyword engine (no external AI).
/// </summary>
public interface IMediaCategorizationService
{
    Task CategorizeAsync(Guid mediaId, CancellationToken cancellationToken = default);
}
