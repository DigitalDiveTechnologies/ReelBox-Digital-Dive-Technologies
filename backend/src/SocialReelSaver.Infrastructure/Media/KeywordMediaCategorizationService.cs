using Microsoft.Extensions.Logging;
using SocialReelSaver.Application.Abstractions.Media;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Domain.Media;

namespace SocialReelSaver.Infrastructure.Media;

/// <summary>
/// Keyword-based <see cref="IMediaCategorizationService"/> (no external AI).
/// Swap DI registration later for Gemini/OpenAI/local models without changing contracts.
/// </summary>
public sealed class KeywordMediaCategorizationService : IMediaCategorizationService
{
    private readonly IMediaRepository _media;
    private readonly ILogger<KeywordMediaCategorizationService> _logger;

    public KeywordMediaCategorizationService(
        IMediaRepository media,
        ILogger<KeywordMediaCategorizationService> logger)
    {
        _media = media;
        _logger = logger;
    }

    public async Task CategorizeAsync(Guid mediaId, CancellationToken cancellationToken = default)
    {
        var item = await _media.GetByIdAsync(mediaId, cancellationToken);
        if (item is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(item.Category))
        {
            return;
        }

        string category;
        try
        {
            category = KeywordCategoryClassifier.Classify(
                item.Title,
                item.Platform.ToString(),
                item.OriginalUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Keyword categorization failed for media {MediaId}; using General", mediaId);
            category = MediaCategories.Default;
        }

        item = await _media.GetByIdAsync(mediaId, cancellationToken);
        if (item is null || !string.IsNullOrWhiteSpace(item.Category))
        {
            return;
        }

        item.Category = category;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await _media.UpdateAsync(item, cancellationToken);
        await _media.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Categorized media {MediaId} as {Category} (keyword)", mediaId, category);
    }
}
