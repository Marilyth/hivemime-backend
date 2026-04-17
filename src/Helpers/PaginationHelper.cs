using Microsoft.EntityFrameworkCore;

public static class PaginationHelper
{
    public static IQueryable<Post> ApplyPaginationFilter(this IQueryable<Post> posts, PostPaginationDto pagination)
    {
        if (pagination.Cursor?.Cursor is null)
            return posts;

        switch (pagination.OrderBy)
        {
            case OrderBy.DateCreated:
                var cursor = DateTimeOffset.Parse(pagination.Cursor.Cursor);
                return pagination.Ascending ?
                    posts.Where(p => p.CreatedAt > cursor || (p.CreatedAt == cursor && p.Id > pagination.Cursor.AfterId)) :
                    posts.Where(p => p.CreatedAt < cursor || (p.CreatedAt == cursor && p.Id > pagination.Cursor.AfterId));
            case OrderBy.Hotness:
                var hotnessCursor = double.Parse(pagination.Cursor.Cursor);
                return pagination.Ascending ?
                    posts.Where(p => p.Hotness > hotnessCursor || (p.Hotness == hotnessCursor && p.Id > pagination.Cursor.AfterId)) :
                    posts.Where(p => p.Hotness < hotnessCursor || (p.Hotness == hotnessCursor && p.Id > pagination.Cursor.AfterId));
            default:
                throw new InvalidOperationException("Invalid order by option.");
        }
    }

    public static IOrderedQueryable<Post> ApplyPaginationOrdering(this IQueryable<Post> posts, PostPaginationDto pagination)
    {
        IOrderedQueryable<Post> orderedPosts;

        switch (pagination.OrderBy)
        {
            case OrderBy.DateCreated:
                orderedPosts = pagination.Ascending ?
                    posts.OrderBy(p => p.CreatedAt) :
                    posts.OrderByDescending(p => p.CreatedAt);
                break;
            case OrderBy.Hotness:
                orderedPosts = pagination.Ascending ?
                    posts.OrderBy(p => p.Hotness) :
                    posts.OrderByDescending(p => p.Hotness);
                break;
            default:
                throw new InvalidOperationException("Invalid order by option.");
        }

        return orderedPosts.ThenBy(p => p.Id);
    }

    public static async Task<IQueryable<Post>> ApplyPaginationSizeAsync(this IQueryable<Post> posts, PostPaginationDto pagination)
    {
        if(pagination.OrderBy == OrderBy.Hotness)
            await posts.UpdateTopKHotnessAsync(heapSize: pagination.PageSize);

        return posts.Take(pagination.PageSize);
    }

    private static async Task UpdateTopKHotnessAsync(this IQueryable<Post> posts, int heapSize)
    {
        double minHotness = double.MinValue;
        DateTimeOffset now = DateTimeOffset.UtcNow;

        while (true)
        {
            var topPosts = posts
                .Where(p => p.Hotness > minHotness)
                .Where(p => p.HotnessLastRecalculatedAt < now)
                .Take(heapSize);

            var ids = await topPosts.Select(p => p.Id).ToListAsync();

            // The minimum has stabilised.
            if (ids.Count < heapSize)
                break;

            await topPosts.ExecuteUpdateAsync(p => p.UpdateHotness());
            minHotness = await posts.Where(p => ids.Contains(p.Id)).MinAsync(p => p.Hotness);
        }

        // The initial query will now be mostly correct.
    }
}