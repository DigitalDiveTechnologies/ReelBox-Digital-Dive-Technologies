namespace SocialReelSaver.Application.Notifications.DTOs;

public sealed record NotificationResponse(
    Guid Id,
    string Title,
    string Message,
    DateTimeOffset CreatedAt,
    bool IsRead,
    Guid? MediaId = null);

public sealed record NotificationListResponse(
    IReadOnlyList<NotificationResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
