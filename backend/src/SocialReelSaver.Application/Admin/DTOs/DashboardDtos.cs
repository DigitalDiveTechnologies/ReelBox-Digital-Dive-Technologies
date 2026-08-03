namespace SocialReelSaver.Application.Admin.DTOs;

public sealed record DashboardSummaryResponse(
    int TotalUsers, int ActiveUsers, int BlockedUsers, int TotalMedia, int CompletedMedia,
    int FailedMedia, int DownloadsToday, decimal SuccessRate, int ActiveAdmins);

public sealed record DashboardTrendPoint(string Date, int Downloads, int Failures);

public sealed record DashboardTrendsResponse(IReadOnlyList<DashboardTrendPoint> Items);

public sealed record DashboardActivityItem(Guid Id, string Type, string Title, DateTimeOffset CreatedAt);

public sealed record DashboardActivityResponse(IReadOnlyList<DashboardActivityItem> Items);
