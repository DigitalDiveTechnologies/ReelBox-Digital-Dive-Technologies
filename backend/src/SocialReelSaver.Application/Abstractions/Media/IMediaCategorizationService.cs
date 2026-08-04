namespace SocialReelSaver.Application.Abstractions.Media;

/// <summary>
/// Pluggable media categorizer (metadata only). Never blocks download.
/// Implementations: keyword engine now; Gemini/OpenAI/local AI can replace via DI later.
/// </summary>
public interface IMediaCategorizationService
{
    Task CategorizeAsync(Guid mediaId, CancellationToken cancellationToken = default);
}
