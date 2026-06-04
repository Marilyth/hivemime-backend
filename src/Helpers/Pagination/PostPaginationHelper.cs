using Microsoft.EntityFrameworkCore;

public class PostPaginationHelper : PaginationHelperBase<Post, PostPaginationDto>
{
    public PostPaginationHelper(PostPaginationDto pagination) : base(pagination)
    {
        IsDescending = pagination.OrderBy is PostOrderBy.New or PostOrderBy.Hot;

        PropertySelector = Pagination.OrderBy switch
        {
            PostOrderBy.New => (Post p) => p.CreatedAt,
            PostOrderBy.Old => (Post p) => p.CreatedAt,
            PostOrderBy.Hot => (Post p) => p.Hotness,
            _ => throw new ValidationException("Invalid order by option.")
        };

        Cursor = Pagination.Cursor is null ? null : Pagination.OrderBy switch
        {
            PostOrderBy.New => DateTimeOffset.Parse(Pagination.Cursor.Cursor),
            PostOrderBy.Old => DateTimeOffset.Parse(Pagination.Cursor.Cursor),
            PostOrderBy.Hot => double.Parse(Pagination.Cursor.Cursor),
            _ => throw new ValidationException("Invalid order by option.")
        };
    }

    protected override IQueryable<Post> ApplyPreFiltering(IQueryable<Post> query)
    {
        if (!string.IsNullOrWhiteSpace(Pagination.Filter))
            query = query.Where(p => p.Polls.Any(poll 
                => poll.SearchVector.Matches(EF.Functions.WebSearchToTsQuery("simple", Pagination.Filter))));

        return query;
    }
}