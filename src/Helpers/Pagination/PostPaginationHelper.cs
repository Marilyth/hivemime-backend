using Mapster;
using Microsoft.EntityFrameworkCore;

public static class PostPaginationHelper
{
    public static IQueryable<Post> ApplyPaginationFilter(this IQueryable<Post> posts, PostPaginationDto pagination)
    {
        if (!string.IsNullOrWhiteSpace(pagination.Filter))
            posts = posts.Where(p => p.Polls.Any(poll 
                => poll.SearchVector.Matches(EF.Functions.WebSearchToTsQuery("simple", pagination.Filter))));

        if (pagination.Cursor is null)
            return posts;

        switch (pagination.OrderBy)
        {
            case PostOrderBy.New:
                DateTimeOffset cursor = DateTimeOffset.Parse(pagination.Cursor.Cursor);
                return posts.Where(p => p.CreatedAt < cursor ||
                                       (p.CreatedAt == cursor && p.Id > pagination.Cursor.Id));
            case PostOrderBy.Old:
                cursor = DateTimeOffset.Parse(pagination.Cursor.Cursor);
                return posts.Where(p => p.CreatedAt > cursor ||
                                       (p.CreatedAt == cursor && p.Id > pagination.Cursor.Id));
            case PostOrderBy.Hot:
                double hotnessCursor = double.Parse(pagination.Cursor.Cursor);
                return posts.Where(p => p.Hotness < hotnessCursor ||
                                       (p.Hotness == hotnessCursor && p.Id > pagination.Cursor.Id));
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

    public static IQueryable<T> ApplyPaginationPageSize<T>(this IQueryable<T> entities, PaginationDto pagination, int maxPageSize = 100)
    {
        pagination.PageSize = Math.Clamp(pagination.PageSize, 1, maxPageSize);
        
        return entities.Take(pagination.PageSize + 1);
    }

    public static async Task<PaginationResultDto<T>> BuildPaginationResultAsync<T>(IQueryable<T> entities, PaginationDto pagination, Func<T, object> cursorSelector) where T : IHasIdentifier
    {
        var result = await entities.ToListAsync();
        T? lastItem = result.Count > pagination.PageSize ? result[pagination.PageSize - 1] : default;
        PaginationCursorDto cursor = lastItem is null ? null :
            new() { Cursor = cursorSelector(lastItem).ToString(), Id = lastItem.Id };

        return new PaginationResultDto<T>
        {
            Items = result.Take(pagination.PageSize).ToList(),
            NextCursor = cursor
        };
    }

    public static async Task<PaginationResultDto<PostDto>> FetchPaginationResultAsync(this IQueryable<Post> entities, PostPaginationDto pagination)
    {
        return await BuildPaginationResultAsync(entities.ProjectToType<PostDto>(), pagination, p => pagination.OrderBy switch
        {
            PostOrderBy.New or PostOrderBy.Old => p.CreatedAt,
            PostOrderBy.Hot => p.Hotness,
            _ => throw new ValidationException("Invalid order by option.")
        });
    }
}