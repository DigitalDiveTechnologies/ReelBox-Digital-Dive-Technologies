using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialReelSaver.Application.Abstractions.Downloading;
using SocialReelSaver.Application.Abstractions.Media;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Abstractions.Providers;
using SocialReelSaver.Application.Abstractions.Queue;
using SocialReelSaver.Application.Abstractions.Storage;
using SocialReelSaver.Application.Media.Errors;
using SocialReelSaver.Application.Media.Jobs;
using SocialReelSaver.Domain.Enums;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Infrastructure.Workers;

/// <summary>
/// Downloader pipeline orchestration (SRS §11) with provider execution framework (FR-007 / FR-018).
/// </summary>
public sealed class MediaDownloadPipeline
{
    private readonly IMediaRepository _media;
    private readonly IMediaStatusService _status;
    private readonly IMediaProviderExecutor _providerExecutor;
    private readonly IMediaDownloader _downloader;
    private readonly IDownloadValidator _validator;
    private readonly IThumbnailService _thumbnails;
    private readonly IObjectStorageFactory _storageFactory;
    private readonly ITemporaryFileManager _tempFiles;
    private readonly IRetryPolicy _retryPolicy;
    private readonly IMediaJobPublisher _publisher;
    private readonly ObjectStorageOptions _storageOptions;
    private readonly ILogger<MediaDownloadPipeline> _logger;

    public MediaDownloadPipeline(
        IMediaRepository media,
        IMediaStatusService status,
        IMediaProviderExecutor providerExecutor,
        IMediaDownloader downloader,
        IDownloadValidator validator,
        IThumbnailService thumbnails,
        IObjectStorageFactory storageFactory,
        ITemporaryFileManager tempFiles,
        IRetryPolicy retryPolicy,
        IMediaJobPublisher publisher,
        IOptions<ObjectStorageOptions> storageOptions,
        ILogger<MediaDownloadPipeline> logger)
    {
        _media = media;
        _status = status;
        _providerExecutor = providerExecutor;
        _downloader = downloader;
        _validator = validator;
        _thumbnails = thumbnails;
        _storageFactory = storageFactory;
        _tempFiles = tempFiles;
        _retryPolicy = retryPolicy;
        _publisher = publisher;
        _storageOptions = storageOptions.Value;
        _logger = logger;
    }

    public async Task ExecuteAsync(MediaDownloadJob job, CancellationToken cancellationToken = default)
    {
        var item = await _media.GetByIdAsync(job.MediaId, cancellationToken);
        if (item is null)
        {
            _logger.LogWarning("Skipping job {JobId}; media {MediaId} not found", job.JobId, job.MediaId);
            return;
        }

        if (item.Status == MediaStatus.Completed)
        {
            _logger.LogInformation("Skipping job {JobId}; media {MediaId} already completed", job.JobId, job.MediaId);
            return;
        }

        string? mediaTempPath = null;
        string? thumbnailTempPath = null;

        try
        {
            await _status.MarkDownloadingAsync(item, cancellationToken);

            // Resolve → Validate → Execute → Validate Result (provider framework)
            var providerOutcome = await _providerExecutor.ExecuteAsync(
                new ProviderContext
                {
                    MediaId = item.Id,
                    JobId = job.JobId,
                    UserId = item.UserId,
                    Platform = item.Platform,
                    OriginalUrl = item.OriginalUrl,
                    Attempt = job.Attempt,
                    CorrelationId = job.JobId.ToString("N"),
                },
                cancellationToken);

            var resolution = providerOutcome.Result;
            if (!resolution.Success || string.IsNullOrWhiteSpace(resolution.ResolvedSourceUrl))
            {
                await FailOrRetryAsync(
                    item,
                    job,
                    ToRetryClassificationCode(resolution),
                    resolution.ErrorMessage ?? "Media source resolution failed.",
                    cancellationToken);
                return;
            }

            if (!string.IsNullOrWhiteSpace(resolution.Title))
            {
                item.Title = resolution.Title;
            }

            // Download pipeline
            var download = await _downloader.DownloadAsync(
                new DownloadContext
                {
                    MediaId = item.Id,
                    JobId = job.JobId,
                    SourceUrl = resolution.ResolvedSourceUrl!,
                    SuggestedFileName = string.IsNullOrWhiteSpace(resolution.SuggestedExtension)
                        ? null
                        : $"media{resolution.SuggestedExtension}",
                    SuggestedMimeType = resolution.SuggestedMimeType,
                    Attempt = job.Attempt,
                },
                cancellationToken);

            if (!download.Success || string.IsNullOrWhiteSpace(download.LocalFilePath))
            {
                await FailOrRetryAsync(
                    item,
                    job,
                    download.ErrorCode ?? "UNKNOWN",
                    download.ErrorMessage ?? "Download failed.",
                    cancellationToken);
                return;
            }

            mediaTempPath = download.LocalFilePath;

            // Validation
            await _status.MarkValidatingAsync(item, cancellationToken);
            var validation = await _validator.ValidateAsync(
                mediaTempPath,
                download.ContentType ?? resolution.SuggestedMimeType,
                resolution.SuggestedDurationMs,
                cancellationToken);

            if (!validation.Success)
            {
                await FailOrRetryAsync(
                    item,
                    job,
                    validation.ErrorCode ?? "INVALID_MEDIA",
                    validation.ErrorMessage ?? "Validation failed.",
                    cancellationToken);
                return;
            }

            item.MimeType = validation.MimeType;
            item.FileSizeBytes = validation.FileSizeBytes;
            item.DurationMs = validation.DurationMs;

            // Thumbnail
            await _status.MarkThumbnailAsync(item, cancellationToken);
            var thumb = await _thumbnails.GenerateAsync(mediaTempPath, item.Id, cancellationToken);
            if (thumb.Success && !string.IsNullOrWhiteSpace(thumb.LocalThumbnailPath))
            {
                thumbnailTempPath = thumb.LocalThumbnailPath;
            }
            else
            {
                // FR-010: absence of thumbnail must not block completion.
                _logger.LogInformation(
                    "Thumbnail skipped for media {MediaId}: {Reason}",
                    item.Id,
                    thumb.ErrorMessage ?? "unavailable");
            }

            // Upload
            await _status.MarkUploadingAsync(item, cancellationToken);
            var storage = _storageFactory.Create();

            await using var mediaStream = File.OpenRead(mediaTempPath);
            var mediaKey = BuildObjectKey(item.UserId, item.Id, "media", GuessExtension(mediaTempPath, validation.MimeType));
            using var uploadCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            uploadCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _storageOptions.UploadTimeoutSeconds)));

            var mediaUpload = await storage.ReplaceAsync(
                new StorageUploadRequest
                {
                    Key = mediaKey,
                    Content = mediaStream,
                    ContentType = validation.MimeType ?? "application/octet-stream",
                    ContentLength = validation.FileSizeBytes,
                },
                uploadCts.Token);

            if (!mediaUpload.Success)
            {
                await FailOrRetryAsync(
                    item,
                    job,
                    mediaUpload.ErrorCode ?? "STORAGE_FAILURE",
                    mediaUpload.ErrorMessage ?? "Media upload failed.",
                    cancellationToken);
                return;
            }

            var storedValidation = await storage.ValidateAsync(
                mediaUpload.Key!,
                validation.MimeType,
                validation.FileSizeBytes,
                cancellationToken);
            if (!storedValidation.Success)
            {
                await FailOrRetryAsync(
                    item,
                    job,
                    storedValidation.ErrorCode ?? "STORAGE_FAILURE",
                    storedValidation.ErrorMessage ?? "Uploaded media failed storage validation.",
                    cancellationToken);
                return;
            }

            string? thumbnailKey = null;
            if (!string.IsNullOrWhiteSpace(thumbnailTempPath))
            {
                await using var thumbStream = File.OpenRead(thumbnailTempPath);
                thumbnailKey = BuildObjectKey(item.UserId, item.Id, "thumb", ".jpg");
                var thumbUpload = await storage.ReplaceAsync(
                    new StorageUploadRequest
                    {
                        Key = thumbnailKey,
                        Content = thumbStream,
                        ContentType = thumb.ContentType ?? "image/jpeg",
                    },
                    uploadCts.Token);

                if (!thumbUpload.Success)
                {
                    _logger.LogWarning(
                        "Thumbnail upload failed for media {MediaId}; continuing without thumbnail",
                        item.Id);
                    thumbnailKey = null;
                }
            }

            // Complete + metadata (FR-011)
            await _status.MarkCompletedAsync(
                item,
                mediaUpload.Key,
                thumbnailKey,
                validation.MimeType,
                validation.FileSizeBytes,
                validation.DurationMs,
                resolution.Title ?? item.Title,
                cancellationToken);

            _logger.LogInformation(
                "Completed pipeline for media {MediaId} key={StorageKey}",
                item.Id,
                mediaUpload.Key);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Unhandled pipeline error for media {MediaId}", item.Id);
            await FailOrRetryAsync(item, job, "UNKNOWN", "Unexpected worker failure.", cancellationToken);
        }
        finally
        {
            await _tempFiles.CleanupAsync(mediaTempPath, CancellationToken.None);
            await _tempFiles.CleanupAsync(thumbnailTempPath, CancellationToken.None);
            await _tempFiles.CleanupMediaTempAsync(item.Id, CancellationToken.None);
        }
    }

    private async Task FailOrRetryAsync(
        Domain.Entities.MediaItem item,
        MediaDownloadJob job,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        var publicCode = SrsMediaErrorCodes.ToPublic(errorCode);

        if (item.Status != MediaStatus.Failed)
        {
            await _status.MarkFailedAsync(item, publicCode, errorMessage, cancellationToken);
        }
        else
        {
            item.ErrorCode = publicCode;
            item.ErrorMessage = errorMessage;
            item.UpdatedAt = DateTimeOffset.UtcNow;
            await _media.UpdateAsync(item, cancellationToken);
            await _media.SaveChangesAsync(cancellationToken);
        }

        // Retry eligibility considers the original internal code when permanent internals map to UNKNOWN.
        if (!_retryPolicy.CanRetry(item.RetryCount, errorCode))
        {
            _logger.LogWarning(
                "Media {MediaId} failed permanently ({ErrorCode}): {Message}",
                item.Id,
                publicCode,
                errorMessage);
            return;
        }

        item.RetryCount += 1;
        var delay = _retryPolicy.GetBackoffDelay(item.RetryCount);
        await _status.MarkQueuedAsync(item, cancellationToken);

        var retryJob = new MediaDownloadJob
        {
            JobId = Guid.NewGuid(),
            MediaId = item.Id,
            UserId = item.UserId,
            Platform = item.Platform,
            OriginalUrl = item.OriginalUrl,
            Attempt = item.RetryCount,
            CreatedAt = DateTimeOffset.UtcNow,
            NotBefore = DateTimeOffset.UtcNow.Add(delay),
        };

        await _publisher.PublishDownloadJobAsync(retryJob, cancellationToken);
        _logger.LogInformation(
            "Scheduled retry {Attempt} for media {MediaId} in {Delay}",
            item.RetryCount,
            item.Id,
            delay);
    }

    private static string ToRetryClassificationCode(ProviderResult resolution) =>
        resolution.ErrorCode switch
        {
            ProviderErrorCode.NotImplemented => "PROVIDER_NOT_IMPLEMENTED",
            ProviderErrorCode.PermanentFailure => "PROVIDER_NOT_IMPLEMENTED",
            ProviderErrorCode.InvalidProviderResponse => "INVALID_PROVIDER_RESPONSE",
            ProviderErrorCode.ConfigurationError => "CONFIGURATION_ERROR",
            ProviderErrorCode.ProviderCancelled => "PROVIDER_CANCELLED",
            ProviderErrorCode.None => resolution.MediaErrorCode ?? SrsMediaErrorCodes.Unknown,
            _ => resolution.MediaErrorCode ?? SrsMediaErrorCodes.Unknown,
        };

    private static string BuildObjectKey(Guid userId, Guid mediaId, string kind, string extension)
    {
        var ext = extension.StartsWith('.') ? extension : "." + extension;
        return $"{userId:N}/{mediaId:N}/{kind}{ext}";
    }

    private static string GuessExtension(string path, string? mime) =>
        Path.GetExtension(path) switch
        {
            { Length: > 0 } ext => ext,
            _ => mime?.ToLowerInvariant() switch
            {
                "video/mp4" => ".mp4",
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                _ => ".bin",
            },
        };
}
