namespace SocialReelSaver.Application.Common;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages =>
        PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public static (int Page, int PageSize) Normalize(int page, int pageSize, int defaultSize = 25, int maxSize = 100)
    {
        var p = page < 1 ? 1 : page;
        var size = pageSize < 1 ? defaultSize : Math.Min(pageSize, maxSize);
        return (p, size);
    }
}
