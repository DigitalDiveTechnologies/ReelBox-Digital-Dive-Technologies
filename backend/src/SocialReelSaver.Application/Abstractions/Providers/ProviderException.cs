namespace SocialReelSaver.Application.Abstractions.Providers;

/// <summary>
/// Exception thrown by provider framework / adapters for structured failure handling.
/// </summary>
public sealed class ProviderException : Exception
{
    public ProviderException(
        ProviderErrorCode errorCode,
        string message,
        string? providerName = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        ProviderName = providerName;
        MediaErrorCode = ProviderErrorMapper.ToMediaErrorCode(errorCode);
    }

    public ProviderErrorCode ErrorCode { get; }

    public string MediaErrorCode { get; }

    public string? ProviderName { get; }
}
