public static class CommentPaginationHelper
{
    public static IQueryable<Comment> ApplyPaginationFilter(this IQueryable<Comment> comments, CommentPaginationDto pagination)
    {
        // TODO 5: Add reverse index. This does not scale well.
        if (pagination.Filter is not null)
        {
            string filter = pagination.Filter.Trim().ToLower();
            comments = comments.Where(c => c.Content.ToLower().Contains(filter));
        }

        if (pagination.Cursor is null)
            return comments;

        var cursor = comments.QueryableFind(pagination.Cursor);
        var intermediateQuery = comments.SelectMany(c => cursor.DefaultIfEmpty(), (c, cursor) => new { Current = c, Cursor = cursor });

        switch (pagination.OrderBy)
        {
            // ToDo: Implement comment scoring.
            case CommentOrderBy.New: case CommentOrderBy.Best:
                return intermediateQuery.Where(c => c.Current.CreatedAt < c.Cursor.CreatedAt ||
                                                   (c.Current.CreatedAt == c.Cursor.CreatedAt && c.Current.Id > c.Cursor.Id))
                    .Select(c => c.Current);
            case CommentOrderBy.Old:
                return intermediateQuery.Where(c => c.Current.CreatedAt > c.Cursor.CreatedAt ||
                                                   (c.Current.CreatedAt == c.Cursor.CreatedAt && c.Current.Id > c.Cursor.Id))
                    .Select(c => c.Current);
            default:
                throw new ValidationException("Invalid order by option.");
        }
    }

    public static IOrderedQueryable<Comment> ApplyPaginationOrdering(this IQueryable<Comment> comments, CommentPaginationDto pagination)
    {
        IOrderedQueryable<Comment> orderedComments;

        switch (pagination.OrderBy)
        {
            // ToDo: Implement comment scoring.
            case CommentOrderBy.New: case CommentOrderBy.Best:
                orderedComments = comments.OrderByDescending(c => c.CreatedAt);
                break;
            case CommentOrderBy.Old:
                orderedComments = comments.OrderBy(c => c.CreatedAt);
                break;
            default:
                throw new ValidationException("Invalid order by option.");
        }

        return orderedComments.ThenBy(c => c.Id);
    }

    public static IQueryable<Comment> ApplyPaginationPageSize(this IQueryable<Comment> comments, CommentPaginationDto pagination, int maxPageSize = 100)
    {
        pagination.PageSize = Math.Clamp(pagination.PageSize, 1, maxPageSize);
        
        return comments.Take(pagination.PageSize);
    }
}