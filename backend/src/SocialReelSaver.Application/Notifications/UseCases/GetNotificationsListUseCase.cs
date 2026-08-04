using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Notifications.DTOs;

namespace SocialReelSaver.Application.Notifications.UseCases;

public sealed class GetNotificationsListUseCase
{
    private readonly INotificationRepository _notifications;

    public GetNotificationsListUseCase(INotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task<NotificationListResponse> HandleAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

        var (items, total) = await _notifications.ListForUserAsync(
            userId,
            page,
            pageSize,
            cancellationToken);

        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);

        return new NotificationListResponse(
            items.Select(n => new NotificationResponse(
                n.Id,
                n.Title,
                n.Message,
                n.CreatedAt,
                n.IsRead,
                n.MediaId)).ToList(),
            page,
            pageSize,
            total,
            totalPages);
    }
}
