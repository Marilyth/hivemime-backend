using Microsoft.EntityFrameworkCore;

public class CommentPaginationHelper : PaginationHelperBase<Comment, CommentPaginationDto>
{
    public CommentPaginationHelper(CommentPaginationDto pagination) : base(pagination)
    {
        IsDescending = pagination.OrderBy is CommentOrderBy.New or CommentOrderBy.Best;

        PropertySelector = pagination.OrderBy switch
        {
            CommentOrderBy.New => (Comment c) => c.CreatedAt,
            CommentOrderBy.Old => (Comment c) => c.CreatedAt,
            CommentOrderBy.Best => (Comment c) => c.CreatedAt, // ToDo: Implement comment scoring.
            _ => throw new ValidationException("Invalid order by option.")
         };

        Cursor = pagination.Cursor is null ? null : pagination.OrderBy switch
        {
            CommentOrderBy.New => DateTimeOffset.Parse(pagination.Cursor.Cursor),
            CommentOrderBy.Old => DateTimeOffset.Parse(pagination.Cursor.Cursor),
            CommentOrderBy.Best => DateTimeOffset.Parse(pagination.Cursor.Cursor), // ToDo: Implement comment scoring.
            _ => throw new ValidationException("Invalid order by option.")
        };
    }

    protected override IQueryable<Comment> ApplyPreFiltering(IQueryable<Comment> query)
    {
        if (!string.IsNullOrWhiteSpace(Pagination.Filter))
        {
            string filter = Pagination.Filter.Trim();
            query = query.Where(c
                => c.SearchVector.Matches(EF.Functions.WebSearchToTsQuery("simple", filter)));
        }

        return query;
    }
}