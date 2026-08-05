namespace AppLogger.API.Models;

using Microsoft.EntityFrameworkCore;

public class PagedList<T>
{
    public List<T> Items { get; }

    public int PageNumber { get; }

    public int PageSize { get; }

    public int? TotalCount { get; }

    public bool HasNextPage => TotalCount is not null && PageNumber * PageSize < TotalCount;

    public bool HasPreviousPage => PageNumber > 1;

    private PagedList(List<T> items, int pageNumber, int pageSize, int? totalCount)
    {
        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public static async Task<PagedList<T>> CreateAsync(IQueryable<T> query, int pageNumber, int pageSize, bool includeCount = true)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be at least 1");
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be at least 1");

        int? totalCount = includeCount ? await query.CountAsync() : null;

        List<T> items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new(items, pageNumber, pageSize, totalCount);
    }
}
