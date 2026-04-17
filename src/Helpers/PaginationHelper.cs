using Microsoft.EntityFrameworkCore;

public static class PaginationHelper
{
    public static async Task<IQueryable<Post>> ApplyPaginationFilterAsync(this IQueryable<Post> posts, PostPaginationDto pagination)
    {
        if (pagination.Cursor is null)
            return posts;

        Post lastPost = await posts.FirstOrExceptionAsync(p => p.Id == pagination.Cursor);

        switch (pagination.OrderBy)
        {
            case OrderBy.Newest:
                return posts.Where(p => p.CreatedAt < lastPost.CreatedAt || (p.CreatedAt == lastPost.CreatedAt && p.Id > lastPost.Id));
            case OrderBy.Oldest:
                return posts.Where(p => p.CreatedAt > lastPost.CreatedAt || (p.CreatedAt == lastPost.CreatedAt && p.Id > lastPost.Id));
            case OrderBy.Hottest:
                return posts.Where(p => p.Hotness < lastPost.Hotness || (p.Hotness == lastPost.Hotness && p.Id > lastPost.Id));
            default:
                throw new InvalidOperationException("Invalid order by option.");
        }
    }

    public static IOrderedQueryable<Post> ApplyPaginationOrdering(this IQueryable<Post> posts, PostPaginationDto pagination)
    {
        IOrderedQueryable<Post> orderedPosts;

        switch (pagination.OrderBy)
        {
            case OrderBy.Newest:
                orderedPosts = posts.OrderByDescending(p => p.CreatedAt);
                break;
            case OrderBy.Oldest:
                orderedPosts = posts.OrderBy(p => p.CreatedAt);
                break;
            case OrderBy.Hottest:
                orderedPosts = posts.OrderByDescending(p => p.Hotness);
                break;
            default:
                throw new InvalidOperationException("Invalid order by option.");
        }

        return orderedPosts.ThenBy(p => p.Id);
    }

    public static async Task<IQueryable<Post>> ApplyPaginationSizeAsync(this IQueryable<Post> posts, PostPaginationDto pagination)
    {
        if(pagination.OrderBy == OrderBy.Hottest)
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