using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Application.Abstractions.Media;

public sealed record MediaUrlAnalysis(
    string OriginalUrl,
    string NormalizedUrl,
    MediaPlatform Platform);

public interface IMediaUrlAnalyzer
{
    /// <summary>
    /// Validates URL syntax/scheme/host and detects Instagram/Facebook platform.
    /// Throws <see cref="Common.Exceptions.BadRequestException"/> on invalid/unsupported URLs.
    /// </summary>
    MediaUrlAnalysis Analyze(string url);
}
