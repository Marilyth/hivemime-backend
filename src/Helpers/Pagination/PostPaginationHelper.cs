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

    public static IQueryable<EntityWithCursorDto<Post>> ToEntityWithCursorDto(this IQueryable<Post> entities, PostPaginationDto pagination)
    {
        return pagination.OrderBy switch
        {
            PostOrderBy.New => entities.Select(p => new EntityWithCursorDto<Post> { Entity = p, Rank = p.CreatedAt }),
            PostOrderBy.Old => entities.Select(p => new EntityWithCursorDto<Post> { Entity = p, Rank = p.CreatedAt }),
            PostOrderBy.Hot => entities.Select(p => new EntityWithCursorDto<Post> { Entity = p, Rank = p.Hotness }),
            _ => throw new ValidationException("Invalid order by option.")
        };
    }

    public static async Task<PaginationResultDto<T>> BuildPaginationResultAsync<T>(this IQueryable entities, PaginationDto pagination)
        where T : IHasIdentifier
    {
        var result = await entities.ProjectToType<EntityWithCursorDto<T>>().ToListAsync();

        EntityWithCursorDto<T>? lastItem = result.Count > pagination.PageSize ? result[pagination.PageSize - 1] : default;
        PaginationCursorDto cursor = lastItem is null ? null :
            new() { Cursor = lastItem.Rank.ToString(), Id = lastItem.Entity.Id };

        return new PaginationResultDto<T>
        {
            Items = result.Take(pagination.PageSize).Select(e => e.Entity).ToList(),
            NextCursor = cursor
        };
    }
}

public class EntityWithCursorDto<T> where T : IHasIdentifier
{
    public T Entity { get; set; }
    public object Rank { get; set; }
}