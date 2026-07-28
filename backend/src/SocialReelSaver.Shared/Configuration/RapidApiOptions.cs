namespace SocialReelSaver.Shared.Configuration;

/// <summary>
/// Full Downloader Social Media (RapidAPI) settings — server-side only.
/// </summary>
public sealed class RapidApiOptions
{
    public const string SectionName = "RapidApi";

    /// <summary>
    /// Base URL without query string (e.g. https://full-downloader-social-media.p.rapidapi.com).
    /// </summary>
    public string BaseUrl { get; set; } = "https://full-downloader-social-media.p.rapidapi.com";

    /// <summary>
    /// Value for the x-rapidapi-host header.
    /// </summary>
    public string Host { get; set; } = "full-downloader-social-media.p.rapidapi.com";

    /// <summary>
    /// RapidAPI subscription key (x-rapidapi-key). Never expose to mobile clients.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
