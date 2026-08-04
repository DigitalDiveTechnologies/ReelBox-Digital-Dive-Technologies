using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialReelSaver.Application.Abstractions.Media;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Domain.Media;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Infrastructure.Media;

/// <summary>
/// Gemini free-tier metadata classifier. Failures store <see cref="MediaCategories.Default"/>.
/// </summary>
public sealed class GeminiMediaCategorizationService : IMediaCategorizationService
{
    public const string HttpClientName = "GeminiCategorizer";

    private readonly IMediaRepository _media;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiMediaCategorizationService> _logger;

    public GeminiMediaCategorizationService(
        IMediaRepository media,
        IHttpClientFactory httpClientFactory,
        IOptions<GeminiOptions> options,
        ILogger<GeminiMediaCategorizationService> logger)
    {
        _media = media;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
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
            category = await ClassifyAsync(item.Title, item.Platform.ToString(), item.OriginalUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini categorization failed for media {MediaId}; using General", mediaId);
            category = MediaCategories.Default;
        }

        // Re-load in case of concurrent updates.
        item = await _media.GetByIdAsync(mediaId, cancellationToken);
        if (item is null || !string.IsNullOrWhiteSpace(item.Category))
        {
            return;
        }

        item.Category = category;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await _media.UpdateAsync(item, cancellationToken);
        await _media.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Categorized media {MediaId} as {Category}", mediaId, category);
    }

    private async Task<string> ClassifyAsync(
        string? title,
        string platform,
        string originalUrl,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogDebug("Gemini ApiKey empty; defaulting category to General");
            return MediaCategories.Default;
        }

        var allowed = string.Join(", ", MediaCategories.All);
        var prompt = new StringBuilder()
            .AppendLine("Classify this social media reel into exactly ONE category.")
            .AppendLine("Return ONLY the category name. No explanation. No JSON. No markdown.")
            .AppendLine($"Allowed categories: {allowed}")
            .AppendLine()
            .AppendLine($"Platform: {platform}")
            .AppendLine($"Title/Caption: {title ?? "(none)"}")
            .AppendLine($"URL: {originalUrl}")
            .ToString();

        var client = _httpClientFactory.CreateClient(HttpClientName);
        var model = string.IsNullOrWhiteSpace(_options.Model) ? "gemini-2.0-flash" : _options.Model.Trim();
        var path = $"models/{model}:generateContent?key={Uri.EscapeDataString(_options.ApiKey)}";

        using var response = await client.PostAsJsonAsync(
            path,
            new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt },
                        },
                    },
                },
                generationConfig = new
                {
                    temperature = 0.1,
                    maxOutputTokens = 32,
                },
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Gemini HTTP {Status} for categorization: {Body}",
                (int)response.StatusCode,
                body.Length > 400 ? body[..400] : body);
            return MediaCategories.Default;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return MediaCategories.NormalizeOrDefault(text);
    }
}
