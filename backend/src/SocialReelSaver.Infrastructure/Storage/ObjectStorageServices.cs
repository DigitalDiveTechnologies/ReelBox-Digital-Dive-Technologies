using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialReelSaver.Application.Abstractions.Storage;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Infrastructure.Storage;

public sealed class LocalObjectStorageService : IObjectStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly ObjectStorageOptions _options;
    private readonly ILogger<LocalObjectStorageService> _logger;

    public LocalObjectStorageService(
        IOptions<ObjectStorageOptions> options,
        ILogger<LocalObjectStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderName => "Local";

    public Task<StorageUploadResult> UploadAsync(
        StorageUploadRequest request,
        CancellationToken cancellationToken = default) =>
        WriteAsync(request, overwrite: false, cancellationToken);

    public Task<StorageUploadResult> ReplaceAsync(
        StorageUploadRequest request,
        CancellationToken cancellationToken = default) =>
        WriteAsync(request, overwrite: true, cancellationToken);

    public Task<StorageDeleteResult> DeleteAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            ValidateKey(key);

            var path = ResolvePath(key);
            var deleted = false;
            if (File.Exists(path))
            {
                File.Delete(path);
                deleted = true;
            }

            var metaPath = GetMetaPath(path);
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
            }

            return Task.FromResult(StorageDeleteResult.Ok(key, deleted));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Local object storage delete failed for key {Key}", key);
            return Task.FromResult(StorageDeleteResult.Failed("STORAGE_FAILURE", "Failed to delete object from local storage.", key));
        }
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateKey(key);
        return Task.FromResult(File.Exists(ResolvePath(key)));
    }

    public async Task<StorageMetadata?> GetMetadataAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateKey(key);

        var path = ResolvePath(key);
        if (!File.Exists(path))
        {
            return null;
        }

        var info = new FileInfo(path);
        var sidecar = await ReadSidecarAsync(path, cancellationToken);
        return new StorageMetadata
        {
            Key = key,
            ContentType = sidecar?.ContentType,
            ContentLength = sidecar?.ContentLength ?? info.Length,
            LastModified = info.LastWriteTimeUtc,
            ETag = sidecar?.ETag ?? ComputeWeakEtag(info),
        };
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateKey(key);

        var path = ResolvePath(key);
        if (!File.Exists(path))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81_920,
            // RandomAccess so HTTP Range / Android ExoPlayer can seek within the file.
            options: FileOptions.Asynchronous | FileOptions.RandomAccess);
        return Task.FromResult<Stream?>(stream);
    }

    public async Task<StorageObject?> OpenObjectAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        var metadata = await GetMetadataAsync(key, cancellationToken);
        if (metadata is null)
        {
            return null;
        }

        var stream = await OpenReadAsync(key, cancellationToken);
        if (stream is null)
        {
            return null;
        }

        return new StorageObject
        {
            Metadata = metadata,
            Content = stream,
        };
    }

    public async Task<StorageValidationResult> ValidateAsync(
        string key,
        string? expectedContentType = null,
        long? expectedContentLength = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return StorageValidationResult.Failed("INVALID_STORAGE_KEY", "Storage key is required.");
        }

        try
        {
            ValidateKey(key);
        }
        catch (InvalidOperationException ex)
        {
            return StorageValidationResult.Failed("INVALID_STORAGE_KEY", ex.Message, key);
        }

        var metadata = await GetMetadataAsync(key, cancellationToken);
        if (metadata is null)
        {
            return StorageValidationResult.Failed("STORAGE_OBJECT_MISSING", "Object does not exist in storage.", key);
        }

        if (!string.IsNullOrWhiteSpace(expectedContentType) &&
            !string.Equals(metadata.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
        {
            return StorageValidationResult.Failed(
                "STORAGE_MIME_MISMATCH",
                $"Expected content type '{expectedContentType}' but found '{metadata.ContentType}'.",
                key);
        }

        if (expectedContentLength is long expected && metadata.ContentLength != expected)
        {
            return StorageValidationResult.Failed(
                "STORAGE_LENGTH_MISMATCH",
                $"Expected content length {expected} but found {metadata.ContentLength}.",
                key);
        }

        if (metadata.ContentLength <= 0)
        {
            return StorageValidationResult.Failed("INVALID_MEDIA", "Stored object is empty.", key);
        }

        return StorageValidationResult.Ok(key, metadata.ContentType, metadata.ContentLength);
    }

    public Task<StorageHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var root = LocalStoragePath.Resolve(_options.LocalRootPath);
            Directory.CreateDirectory(root);

            var probe = Path.Combine(root, $".health-{Guid.NewGuid():N}");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);

            return Task.FromResult(StorageHealthResult.Ok(
                ProviderName,
                new Dictionary<string, string>
                {
                    ["root"] = root,
                    ["writable"] = "true",
                }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(StorageHealthResult.Unhealthy(
                ProviderName,
                $"Local object storage unavailable: {ex.Message}"));
        }
    }

    private async Task<StorageUploadResult> WriteAsync(
        StorageUploadRequest request,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        try
        {
            ValidateKey(request.Key);
            if (string.IsNullOrWhiteSpace(request.ContentType))
            {
                return StorageUploadResult.Failed("INVALID_MEDIA", "Content type is required for upload.");
            }

            var path = ResolvePath(request.Key);
            if (!overwrite && File.Exists(path))
            {
                return StorageUploadResult.Failed("STORAGE_OBJECT_EXISTS", $"Object '{request.Key}' already exists.");
            }

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var file = new FileStream(
                path,
                overwrite ? FileMode.Create : FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81_920,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);

            await request.Content.CopyToAsync(file, cancellationToken);
            await file.FlushAsync(cancellationToken);
            var length = file.Length;

            if (request.ContentLength is long expected && expected != length)
            {
                await file.DisposeAsync();
                File.Delete(path);
                return StorageUploadResult.Failed(
                    "STORAGE_LENGTH_MISMATCH",
                    $"Uploaded length {length} did not match declared content length {expected}.");
            }

            var metadata = new StorageMetadata
            {
                Key = request.Key,
                ContentType = request.ContentType,
                ContentLength = length,
                LastModified = DateTimeOffset.UtcNow,
                ETag = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{request.Key}:{length}"))).ToLowerInvariant(),
            };

            await WriteSidecarAsync(path, metadata, cancellationToken);
            _logger.LogInformation("Stored object locally at key {Key} ({Bytes} bytes)", request.Key, length);
            return StorageUploadResult.Ok(request.Key, metadata);
        }
        catch (IOException ex) when (!overwrite)
        {
            return StorageUploadResult.Failed("STORAGE_OBJECT_EXISTS", ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Local object storage upload failed for key {Key}", request.Key);
            return StorageUploadResult.Failed("STORAGE_FAILURE", "Failed to write media to local object storage.");
        }
    }

    private async Task WriteSidecarAsync(string objectPath, StorageMetadata metadata, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new Sidecar(metadata.ContentType, metadata.ContentLength, metadata.ETag), JsonOptions);
        await File.WriteAllTextAsync(GetMetaPath(objectPath), payload, cancellationToken);
    }

    private async Task<Sidecar?> ReadSidecarAsync(string objectPath, CancellationToken cancellationToken)
    {
        var metaPath = GetMetaPath(objectPath);
        if (!File.Exists(metaPath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(metaPath, cancellationToken);
        return JsonSerializer.Deserialize<Sidecar>(json, JsonOptions);
    }

    private static string GetMetaPath(string objectPath) => objectPath + ".meta.json";

    private static string ComputeWeakEtag(FileInfo info) =>
        $"W/\"{info.Length}-{info.LastWriteTimeUtc.Ticks}\"";

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Storage key is required.");
        }

        if (key.Contains("..", StringComparison.Ordinal) || key.StartsWith('/') || key.StartsWith('\\'))
        {
            throw new InvalidOperationException("Storage key is invalid.");
        }
    }

    private string ResolvePath(string key)
    {
        var root = LocalStoragePath.Resolve(_options.LocalRootPath);
        Directory.CreateDirectory(root);
        var safeKey = key.Replace('\\', '/').TrimStart('/');
        var combined = Path.GetFullPath(Path.Combine(root, safeKey.Replace('/', Path.DirectorySeparatorChar)));
        if (!combined.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(combined, root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Object storage key escapes the configured local root.");
        }

        return combined;
    }

    private sealed record Sidecar(string? ContentType, long ContentLength, string? ETag);
}

/// <summary>
/// Production-ready S3-compatible placeholder (no AWS SDK in this sprint).
/// </summary>
public sealed class S3CompatibleObjectStorageService : IObjectStorageService
{
    private readonly ObjectStorageOptions _options;

    public S3CompatibleObjectStorageService(IOptions<ObjectStorageOptions> options)
    {
        _options = options.Value;
    }

    public string ProviderName => "S3Compatible";

    public Task<StorageUploadResult> UploadAsync(StorageUploadRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // TODO: Implement S3-compatible PutObject (SRS FR-009 / NFR-004).
        _ = _options;
        return Task.FromResult(StorageUploadResult.NotImplemented(ProviderName));
    }

    public Task<StorageUploadResult> ReplaceAsync(StorageUploadRequest request, CancellationToken cancellationToken = default) =>
        UploadAsync(request, cancellationToken);

    public Task<StorageDeleteResult> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // TODO: Implement S3 DeleteObject.
        return Task.FromResult(StorageDeleteResult.NotImplemented(ProviderName, key));
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // TODO: Implement S3 HeadObject.
        return Task.FromResult(false);
    }

    public Task<StorageMetadata?> GetMetadataAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<StorageMetadata?>(null);
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // TODO: Implement S3 GetObject stream.
        return Task.FromResult<Stream?>(null);
    }

    public Task<StorageObject?> OpenObjectAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult<StorageObject?>(null);

    public Task<StorageValidationResult> ValidateAsync(
        string key,
        string? expectedContentType = null,
        long? expectedContentLength = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(StorageValidationResult.Failed(
            "STORAGE_NOT_IMPLEMENTED",
            $"{ProviderName} validation is not implemented yet.",
            key));
    }

    public Task<StorageHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(StorageHealthResult.Unhealthy(
            ProviderName,
            "S3-compatible storage client is not implemented yet.",
            new Dictionary<string, string>
            {
                ["serviceUrl"] = _options.ServiceUrl,
                ["bucket"] = _options.BucketName,
            }));
    }
}

/// <summary>
/// Cloudflare R2 placeholder (no Cloudflare/AWS SDK in this sprint).
/// </summary>
public sealed class CloudflareR2StorageService : IObjectStorageService
{
    private readonly ObjectStorageOptions _options;

    public CloudflareR2StorageService(IOptions<ObjectStorageOptions> options)
    {
        _options = options.Value;
    }

    public string ProviderName => "CloudflareR2";

    public Task<StorageUploadResult> UploadAsync(StorageUploadRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // TODO: Implement Cloudflare R2 upload via S3-compatible API (SRS §18).
        _ = _options;
        return Task.FromResult(StorageUploadResult.NotImplemented(ProviderName));
    }

    public Task<StorageUploadResult> ReplaceAsync(StorageUploadRequest request, CancellationToken cancellationToken = default) =>
        UploadAsync(request, cancellationToken);

    public Task<StorageDeleteResult> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // TODO: Implement R2 DeleteObject.
        return Task.FromResult(StorageDeleteResult.NotImplemented(ProviderName, key));
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(false);
    }

    public Task<StorageMetadata?> GetMetadataAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<StorageMetadata?>(null);
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<Stream?>(null);
    }

    public Task<StorageObject?> OpenObjectAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult<StorageObject?>(null);

    public Task<StorageValidationResult> ValidateAsync(
        string key,
        string? expectedContentType = null,
        long? expectedContentLength = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(StorageValidationResult.Failed(
            "STORAGE_NOT_IMPLEMENTED",
            $"{ProviderName} validation is not implemented yet.",
            key));
    }

    public Task<StorageHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(StorageHealthResult.Unhealthy(
            ProviderName,
            "Cloudflare R2 storage client is not implemented yet.",
            new Dictionary<string, string>
            {
                ["serviceUrl"] = _options.ServiceUrl,
                ["bucket"] = _options.BucketName,
            }));
    }
}

public sealed class StorageFactory : IObjectStorageFactory
{
    private readonly IServiceProvider _services;
    private readonly ObjectStorageOptions _options;

    public StorageFactory(IServiceProvider services, IOptions<ObjectStorageOptions> options)
    {
        _services = services;
        _options = options.Value;
    }

    public IObjectStorageService Create() => Create(_options.Provider);

    public IObjectStorageService Create(string providerName)
    {
        return providerName.Trim().ToLowerInvariant() switch
        {
            "local" => GetRequired<LocalObjectStorageService>(),
            "s3" or "s3compatible" or "minio" => GetRequired<S3CompatibleObjectStorageService>(),
            "r2" or "cloudflarer2" or "cloudflare" => GetRequired<CloudflareR2StorageService>(),
            _ => throw new InvalidOperationException($"Unsupported object storage provider '{providerName}'."),
        };
    }

    private T GetRequired<T>() where T : class =>
        (T?)_services.GetService(typeof(T))
        ?? throw new InvalidOperationException($"Service {typeof(T).Name} is not registered.");
}

public sealed class ObjectStorageHealthCheck : IHealthCheck
{
    private readonly IObjectStorageFactory _factory;

    public ObjectStorageHealthCheck(IObjectStorageFactory factory)
    {
        _factory = factory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var storage = _factory.Create();
        var result = await storage.CheckHealthAsync(cancellationToken);
        var data = result.Diagnostics is null
            ? null
            : result.Diagnostics.ToDictionary(kv => kv.Key, kv => (object)kv.Value);

        if (result.Healthy && result.Available)
        {
            return HealthCheckResult.Healthy(result.Message, data);
        }

        return HealthCheckResult.Unhealthy(result.Message, data: data);
    }
}
