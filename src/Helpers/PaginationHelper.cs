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
            case OrderBy.VoteCount:
                var voteCursor = int.Parse(pagination.Cursor.Cursor);
                return pagination.Ascending ?
                    posts.Where(p => p.VoteCount > voteCursor || (p.VoteCount == voteCursor && p.Id > pagination.Cursor.AfterId)) :
                    posts.Where(p => p.VoteCount < voteCursor || (p.VoteCount == voteCursor && p.Id > pagination.Cursor.AfterId));
            case OrderBy.CommentCount:
                var commentCursor = int.Parse(pagination.Cursor.Cursor);
                return pagination.Ascending ?
                    posts.Where(p => p.CommentCount > commentCursor || (p.CommentCount == commentCursor && p.Id > pagination.Cursor.AfterId)) :
                    posts.Where(p => p.CommentCount < commentCursor || (p.CommentCount == commentCursor && p.Id > pagination.Cursor.AfterId));
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
            case OrderBy.VoteCount:
                orderedPosts = pagination.Ascending ?
                    posts.OrderBy(p => p.VoteCount) :
                    posts.OrderByDescending(p => p.VoteCount);
                break;
            case OrderBy.CommentCount:
                orderedPosts = pagination.Ascending ?
                    posts.OrderBy(p => p.CommentCount) :
                    posts.OrderByDescending(p => p.CommentCount);
                break;
            default:
                throw new InvalidOperationException("Invalid order by option.");
        }

        return orderedPosts.ThenBy(p => p.Id);
    }
}