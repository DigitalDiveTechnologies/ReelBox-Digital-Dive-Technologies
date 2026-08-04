namespace SocialReelSaver.Application.Abstractions.Media;

/// <summary>Background AI categorizer (metadata only). Never blocks download.</summary>
public interface IMediaCategorizationService
{
    Task CategorizeAsync(Guid mediaId, CancellationToken cancellationToken = default);
}
