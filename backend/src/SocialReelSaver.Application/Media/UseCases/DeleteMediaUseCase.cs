using Microsoft.Extensions.Logging;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Abstractions.Storage;
using SocialReelSaver.Application.Common.Exceptions;

namespace SocialReelSaver.Application.Media.UseCases;

public sealed class DeleteMediaUseCase
{
    private readonly IMediaRepository _media;
    private readonly IObjectStorageService _storage;
    private readonly ILogger<DeleteMediaUseCase> _logger;

    public DeleteMediaUseCase(
        IMediaRepository media,
        IObjectStorageService storage,
        ILogger<DeleteMediaUseCase> logger)
    {
        _media = media;
        _storage = storage;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid userId,
        Guid mediaId,
        CancellationToken cancellationToken = default)
    {
        var item = await _media.GetByIdForUserAsync(mediaId, userId, cancellationToken);
        if (item is null)
        {
            throw new NotFoundException("Media item not found.");
        }

        // SRS FR-016 / AC-12 — remove managed video + thumbnail objects, then metadata.
        await DeleteObjectAsync(item.MediaStorageKey, cancellationToken);
        await DeleteObjectAsync(item.ThumbnailStorageKey, cancellationToken);

        await _media.DeleteAsync(item, cancellationToken);
        await _media.SaveChangesAsync(cancellationToken);
    }

    private async Task DeleteObjectAsync(string? key, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        var result = await _storage.DeleteAsync(key, cancellationToken);
        if (!result.Success && !result.IsNotImplemented)
        {
            _logger.LogWarning(
                "Failed to delete storage object {Key}: {Code} {Message}",
                key,
                result.ErrorCode,
                result.ErrorMessage);
        }
    }
}
