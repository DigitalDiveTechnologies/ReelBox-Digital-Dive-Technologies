namespace SocialReelSaver.Application.Abstractions.Storage;

public sealed record StorageMetadata
{
    public required string Key { get; init; }

    public string? ContentType { get; init; }

    public long ContentLength { get; init; }

    public DateTimeOffset? LastModified { get; init; }

    public string? ETag { get; init; }
}

public sealed record StorageObject
{
    public required StorageMetadata Metadata { get; init; }

    public required Stream Content { get; init; }
}

public sealed record StorageUploadRequest
{
    public required string Key { get; init; }

    public required Stream Content { get; init; }

    public required string ContentType { get; init; }

    public long? ContentLength { get; init; }
}

public sealed record StorageUploadResult
{
    public bool Success { get; init; }

    public string? Key { get; init; }

    public StorageMetadata? Metadata { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public bool IsNotImplemented { get; init; }

    public static StorageUploadResult Ok(string key, StorageMetadata? metadata = null) => new()
    {
        Success = true,
        Key = key,
        Metadata = metadata,
    };

    public static StorageUploadResult Failed(string code, string message) => new()
    {
        Success = false,
        ErrorCode = code,
        ErrorMessage = message,
    };

    public static StorageUploadResult NotImplemented(string providerName) => new()
    {
        Success = false,
        IsNotImplemented = true,
        ErrorCode = "STORAGE_NOT_IMPLEMENTED",
        ErrorMessage = $"{providerName} is configured but not implemented yet.",
    };
}

public sealed record StorageDeleteResult
{
    public bool Success { get; init; }

    public string? Key { get; init; }

    public bool Deleted { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public bool IsNotImplemented { get; init; }

    public static StorageDeleteResult Ok(string key, bool deleted) => new()
    {
        Success = true,
        Key = key,
        Deleted = deleted,
    };

    public static StorageDeleteResult Failed(string code, string message, string? key = null) => new()
    {
        Success = false,
        Key = key,
        ErrorCode = code,
        ErrorMessage = message,
    };

    public static StorageDeleteResult NotImplemented(string providerName, string key) => new()
    {
        Success = false,
        IsNotImplemented = true,
        Key = key,
        ErrorCode = "STORAGE_NOT_IMPLEMENTED",
        ErrorMessage = $"{providerName} is configured but not implemented yet.",
    };
}

public sealed record StorageValidationResult
{
    public bool Success { get; init; }

    public string? Key { get; init; }

    public bool Exists { get; init; }

    public string? ContentType { get; init; }

    public long? ContentLength { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public static StorageValidationResult Ok(
        string key,
        string? contentType,
        long contentLength) => new()
    {
        Success = true,
        Key = key,
        Exists = true,
        ContentType = contentType,
        ContentLength = contentLength,
    };

    public static StorageValidationResult Failed(string code, string message, string? key = null) => new()
    {
        Success = false,
        Key = key,
        ErrorCode = code,
        ErrorMessage = message,
    };
}

public sealed record StorageHealthResult
{
    public bool Healthy { get; init; }

    public bool Available { get; init; }

    public string ProviderName { get; init; } = string.Empty;

    public string? Message { get; init; }

    public IReadOnlyDictionary<string, string>? Diagnostics { get; init; }

    public static StorageHealthResult Ok(string provider, IReadOnlyDictionary<string, string>? diagnostics = null) =>
        new()
        {
            Healthy = true,
            Available = true,
            ProviderName = provider,
            Message = "Object storage is available.",
            Diagnostics = diagnostics,
        };

    public static StorageHealthResult Unhealthy(
        string provider,
        string message,
        IReadOnlyDictionary<string, string>? diagnostics = null) =>
        new()
        {
            Healthy = false,
            Available = false,
            ProviderName = provider,
            Message = message,
            Diagnostics = diagnostics,
        };
}

/// <summary>
/// Object storage abstraction (SRS FR-009 / NFR-004 / §18).
/// </summary>
public interface IObjectStorageService
{
    string ProviderName { get; }

    Task<StorageUploadResult> UploadAsync(
        StorageUploadRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces an existing object (overwrite semantics).
    /// </summary>
    Task<StorageUploadResult> ReplaceAsync(
        StorageUploadRequest request,
        CancellationToken cancellationToken = default);

    Task<StorageDeleteResult> DeleteAsync(
        string key,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    Task<StorageMetadata?> GetMetadataAsync(string key, CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default);

    Task<StorageObject?> OpenObjectAsync(string key, CancellationToken cancellationToken = default);

    Task<StorageValidationResult> ValidateAsync(
        string key,
        string? expectedContentType = null,
        long? expectedContentLength = null,
        CancellationToken cancellationToken = default);

    Task<StorageHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}

public interface IObjectStorageFactory
{
    IObjectStorageService Create();

    IObjectStorageService Create(string providerName);
}
