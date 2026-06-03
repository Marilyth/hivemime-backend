using Mapster;
using Microsoft.EntityFrameworkCore;

public static class CommentPaginationHelper
{
    public static IQueryable<Comment> ApplyPaginationFilter(this IQueryable<Comment> comments, CommentPaginationDto pagination)
    {
        if (pagination.Filter is not null)
        {
            comments = comments.Where(c
                => c.SearchVector.Matches(EF.Functions.WebSearchToTsQuery("simple", pagination.Filter)));
        }

        if (pagination.Cursor is null)
            return comments;

        switch (pagination.OrderBy)
        {
            // ToDo: Implement comment scoring.
            case CommentOrderBy.New: case CommentOrderBy.Best:
                DateTimeOffset newCursor = DateTimeOffset.Parse(pagination.Cursor.Cursor);
                return comments.Where(c => c.CreatedAt < newCursor ||
                                           (c.CreatedAt == newCursor && c.Id > pagination.Cursor.Id));
            case CommentOrderBy.Old:
                DateTimeOffset oldCursor = DateTimeOffset.Parse(pagination.Cursor.Cursor);
                return comments.Where(c => c.CreatedAt > oldCursor ||
                                           (c.CreatedAt == oldCursor && c.Id > pagination.Cursor.Id));
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

    public static IQueryable<EntityWithCursorDto<Comment>> ToEntityWithCursorDto(this IQueryable<Comment> entities, CommentPaginationDto pagination)
    {
        return pagination.OrderBy switch
        {
            CommentOrderBy.New => entities.Select(c => new EntityWithCursorDto<Comment> { Entity = c, Rank = c.CreatedAt }),
            CommentOrderBy.Old => entities.Select(c => new EntityWithCursorDto<Comment> { Entity = c, Rank = c.CreatedAt }),
            CommentOrderBy.Best => entities.Select(c => new EntityWithCursorDto<Comment> { Entity = c, Rank = c.CreatedAt }), // ToDo: Implement comment scoring.
            _ => throw new ValidationException("Invalid order by option.")
        };
    }
}