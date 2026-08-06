using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialReelSaver.Application.Media.DTOs;
using SocialReelSaver.Application.Media.UseCases;
using SocialReelSaver.Domain.Enums;

namespace SocialReelSaver.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/media")]
public sealed class MediaController : ControllerBase
{
    private readonly CreateMediaUseCase _create;
    private readonly GetMediaListUseCase _list;
    private readonly GetMediaByIdUseCase _getById;
    private readonly RetryMediaUseCase _retry;
    private readonly DeleteMediaUseCase _delete;
    private readonly GetPlaybackUseCase _playback;
    private readonly GetMediaContentUseCase _content;

    public MediaController(
        CreateMediaUseCase create,
        GetMediaListUseCase list,
        GetMediaByIdUseCase getById,
        RetryMediaUseCase retry,
        DeleteMediaUseCase delete,
        GetPlaybackUseCase playback,
        GetMediaContentUseCase content)
    {
        _create = create;
        _list = list;
        _getById = getById;
        _retry = retry;
        _delete = delete;
        _playback = playback;
        _content = content;
    }

    [HttpPost]
    [ProducesResponseType(typeof(MediaResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(MediaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateMediaRequest request,
        IValidator<CreateMediaRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var userId = GetUserId();
        var (response, created) = await _create.HandleAsync(userId, request, cancellationToken);

        if (!created)
        {
            return Ok(response);
        }

        return Accepted($"/api/v1/media/{response.Id}", response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(MediaListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? platform = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var statusFilter = ParseStatus(status);
        var platformFilter = ParsePlatform(platform);

        var result = await _list.HandleAsync(
            userId,
            page,
            pageSize,
            statusFilter,
            platformFilter,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MediaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getById.HandleAsync(GetUserId(), id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/retry")]
    [ProducesResponseType(typeof(MediaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Retry(Guid id, CancellationToken cancellationToken)
    {
        var result = await _retry.HandleAsync(GetUserId(), id, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _delete.HandleAsync(GetUserId(), id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/playback")]
    [ProducesResponseType(typeof(PlaybackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Playback(Guid id, CancellationToken cancellationToken)
    {
        var result = await _playback.HandleAsync(GetUserId(), id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Streams media bytes for a signed local application playback URL (SRS FR-014).
    /// </summary>
    [HttpGet("{id:guid}/content")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Content(
        Guid id,
        [FromQuery] Guid uid,
        [FromQuery] string key,
        [FromQuery] long exp,
        [FromQuery] string sig,
        CancellationToken cancellationToken)
    {
        var result = await _content.HandleAsync(id, uid, key, exp, sig, cancellationToken);

        // PhysicalFile + range processing is required for Android video_player / ExoPlayer
        // (Range: bytes=… → 206 Partial Content). A plain non-seekable stream breaks playback.
        if (result.Content is FileStream fileStream)
        {
            var path = fileStream.Name;
            await fileStream.DisposeAsync();
            return PhysicalFile(path, result.ContentType, enableRangeProcessing: true);
        }

        return File(result.Content, result.ContentType, enableRangeProcessing: true);
    }

    private Guid GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (raw is null || !Guid.TryParse(raw, out var userId))
        {
            throw new UnauthorizedAccessException("Authenticated user id is missing.");
        }

        return userId;
    }

    private static MediaStatus? ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        return Enum.TryParse<MediaStatus>(status, ignoreCase: true, out var parsed)
            ? parsed
            : throw new Application.Common.Exceptions.BadRequestException(
                $"Unsupported status filter '{status}'.",
                Application.Media.Errors.SrsMediaErrorCodes.Unknown);
    }

    private static MediaPlatform? ParsePlatform(string? platform)
    {
        if (string.IsNullOrWhiteSpace(platform))
        {
            return null;
        }

        return Enum.TryParse<MediaPlatform>(platform, ignoreCase: true, out var parsed)
            ? parsed
            : throw new Application.Common.Exceptions.BadRequestException(
                $"Unsupported platform filter '{platform}'.",
                Application.Media.Errors.SrsMediaErrorCodes.Unknown);
    }
}
