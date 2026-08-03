using SocialReelSaver.Application.Abstractions.Admin;
using SocialReelSaver.Application.Admin.DTOs;
using SocialReelSaver.Application.Common;
using SocialReelSaver.Application.Common.Exceptions;

namespace SocialReelSaver.Application.Admin.UseCases;

public sealed class ListAppErrorLogsUseCase(IAppErrorLogRepository logs)
{
    public async Task<PagedResult<AppErrorLogListItem>> HandleAsync(
        int page, int pageSize, string? search, string? level, string? correlationId,
        DateTimeOffset? fromUtc, DateTimeOffset? toUtc, string? sortBy, string? sortDir,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PagedResult<AppErrorLogListItem>.Normalize(page, pageSize);
        var result = await logs.ListAsync(page, pageSize, search, level, correlationId, fromUtc, toUtc, sortBy, sortDir, cancellationToken);
        return new(result.Items.Select(x => new AppErrorLogListItem(
            x.Id, x.Level, x.Message, x.Source, x.CorrelationId, x.Path, x.StatusCode, x.CreatedAt)).ToList(),
            page, pageSize, result.TotalCount);
    }
}

public sealed class GetAppErrorLogUseCase(IAppErrorLogRepository logs)
{
    public async Task<AppErrorLogDetailResponse> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var x = await logs.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Error log not found.");
        return new(x.Id, x.Level, x.Message, x.Detail, x.Source, x.CorrelationId, x.Path, x.StatusCode, x.CreatedAt);
    }
}
