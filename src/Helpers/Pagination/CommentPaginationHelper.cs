using Mapster;
using Microsoft.EntityFrameworkCore;

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

    public static async Task<PaginationResultDto<CommentDto>> FetchPaginationResultAsync(this IQueryable<Comment> entities, CommentPaginationDto pagination)
    {
        return await PostPaginationHelper.BuildPaginationResultAsync(entities.ProjectToType<CommentDto>(), pagination, c => pagination.OrderBy switch
        {
            CommentOrderBy.New or CommentOrderBy.Old => c.CreatedAt,
            CommentOrderBy.Best => c.CreatedAt, // ToDo: Implement comment scoring.
            _ => throw new ValidationException("Invalid order by option.")
        });
    }
}