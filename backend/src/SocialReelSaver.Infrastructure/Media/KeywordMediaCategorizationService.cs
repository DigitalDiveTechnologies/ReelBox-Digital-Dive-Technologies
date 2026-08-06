using Microsoft.Extensions.Logging;
using SocialReelSaver.Application.Abstractions.Media;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Domain.Media;
using SocialReelSaver.Infrastructure.Media.Categorization;

namespace SocialReelSaver.Infrastructure.Media;

/// <summary>
/// Offline keyword-only categorizer. No Gemini/LLM/embeddings or external AI calls.
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
        if (item is null || !string.IsNullOrWhiteSpace(item.Category))
        {
            return;
        }

        CategoryScoreResult result;
        try
        {
            var signals = CategorizationSignals.FromMediaItem(item);
            result = WeightedKeywordCategoryEngine.Classify(signals);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Keyword categorization failed for media {MediaId}; using General", mediaId);
            result = new CategoryScoreResult(MediaCategories.Default, 0.1, 0, ClassificationSources.KeywordEngine);
        }

        item = await _media.GetByIdAsync(mediaId, cancellationToken);
        if (item is null || !string.IsNullOrWhiteSpace(item.Category))
        {
            return;
        }

        item.Category = result.Category;
        item.CategoryConfidence = Math.Round(result.Confidence, 4);
        item.ClassificationSource = ClassificationSources.KeywordEngine;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await _media.UpdateAsync(item, cancellationToken);
        await _media.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Categorized media {MediaId} as {Category} (KeywordEngine, score {Score}, confidence {Confidence:0.00})",
            mediaId,
            result.Category,
            result.RawScore,
            result.Confidence);
    }
}
