public static class PostPaginationHelper
{
    public static IQueryable<Post> ApplyPaginationFilter(this IQueryable<Post> posts, PostPaginationDto pagination)
    {
        // TODO 5: Add reverse index. This does not scale well.
        if (!string.IsNullOrWhiteSpace(pagination.Filter))
        {
            string filter = pagination.Filter.Trim().ToLower();

            posts = posts.Where(p => p.Polls.Any(poll => poll.Title.ToLower().Contains(filter)
                                  || poll.Description.ToLower().Contains(filter)));
        }

        if (pagination.Cursor is null)
            return posts;

        var cursor = posts.QueryableFind(pagination.Cursor);
        var intermediateQuery = posts.SelectMany(p => cursor.DefaultIfEmpty(), (p, cursor) => new { Current = p, Cursor = cursor });

        switch (pagination.OrderBy)
        {
            case PostOrderBy.New:
                return intermediateQuery.Where(p => p.Current.CreatedAt < p.Cursor.CreatedAt ||
                                                   (p.Current.CreatedAt == p.Cursor.CreatedAt && p.Current.Id > p.Cursor.Id))
                    .Select(p => p.Current);
            case PostOrderBy.Old:
                return intermediateQuery.Where(p => p.Current.CreatedAt > p.Cursor.CreatedAt ||
                                                   (p.Current.CreatedAt == p.Cursor.CreatedAt && p.Current.Id > p.Cursor.Id))
                    .Select(p => p.Current);
            case PostOrderBy.Hot:
                return intermediateQuery.Where(p => p.Current.Hotness < p.Cursor.Hotness ||
                                                   (p.Current.Hotness == p.Cursor.Hotness && p.Current.Id > p.Cursor.Id))
                    .Select(p => p.Current);
            default:
                throw new ValidationException("Invalid order by option.");
        }
    }

    public static IOrderedQueryable<Post> ApplyPaginationOrdering(this IQueryable<Post> posts, PostPaginationDto pagination)
    {
        IOrderedQueryable<Post> orderedPosts;

        switch (pagination.OrderBy)
        {
            case PostOrderBy.New:
                orderedPosts = posts.OrderByDescending(p => p.CreatedAt);
                break;
            case PostOrderBy.Old:
                orderedPosts = posts.OrderBy(p => p.CreatedAt);
                break;
            case PostOrderBy.Hot:
                orderedPosts = posts.OrderByDescending(p => p.Hotness);
                break;
            default:
                throw new ValidationException("Invalid order by option.");
        }

        return orderedPosts.ThenBy(p => p.Id);
    }

    public static IQueryable<Post> ApplyPaginationPageSize(this IQueryable<Post> posts, PostPaginationDto pagination, int maxPageSize = 100)
    {
        pagination.PageSize = Math.Clamp(pagination.PageSize, 1, maxPageSize);
        
        return posts.Take(pagination.PageSize);
    }
}