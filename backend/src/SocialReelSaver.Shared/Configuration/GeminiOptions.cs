namespace SocialReelSaver.Shared.Configuration;

/// <summary>
/// Google Gemini (free-tier) settings — server-side only. Never expose to Flutter.
/// </summary>
public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    /// <summary>Generative Language API base (no trailing slash).</summary>
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";

    /// <summary>Free production model id (e.g. gemini-2.0-flash or gemini-1.5-flash).</summary>
    public string Model { get; set; } = "gemini-2.0-flash";

    /// <summary>API key from Google AI Studio. Empty disables AI (falls back to General).</summary>
    public string ApiKey { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 20;
}
