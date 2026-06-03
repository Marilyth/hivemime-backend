using Mapster;
using Microsoft.EntityFrameworkCore;

public static class HivePaginationHelper
{
    public static IQueryable<Hive> ApplyPaginationFilter(this IQueryable<Hive> hives, HivePaginationDto pagination)
    {
        if (pagination.Filter is not null)
        {
            string filter = pagination.Filter.Trim();
            hives = hives.Where(h => h.Name.StartsWith(filter) ||
                                     h.SearchVector.Matches(EF.Functions.WebSearchToTsQuery("simple", filter)));
        }
        
        if (pagination.Cursor is null)
            return hives;

        switch (pagination.OrderBy)
        {
            // ToDo: Implement hive scoring.
            case HiveOrderBy.New:
                DateTimeOffset newCursor = DateTimeOffset.Parse(pagination.Cursor.Cursor);
                return hives.Where(h => h.CreatedAt < newCursor ||
                                       (h.CreatedAt == newCursor && h.Id > pagination.Cursor.Id));
            case HiveOrderBy.Old:
                DateTimeOffset oldCursor = DateTimeOffset.Parse(pagination.Cursor.Cursor);
                return hives.Where(h => h.CreatedAt > oldCursor ||
                                       (h.CreatedAt == oldCursor && h.Id > pagination.Cursor.Id));
            case HiveOrderBy.Users:
                int usersCursor = int.Parse(pagination.Cursor.Cursor);
                return hives.Where(h => h.UserCount < usersCursor ||
                                       (h.UserCount == usersCursor && h.Id > pagination.Cursor.Id));
            default:
                throw new ValidationException("Invalid order by option.");
        }
    }

    public static IOrderedQueryable<Hive> ApplyPaginationOrdering(this IQueryable<Hive> hives, HivePaginationDto pagination)
    {
        IOrderedQueryable<Hive> orderedHives;

        switch (pagination.OrderBy)
        {
            // ToDo: Implement hive scoring.
            case HiveOrderBy.New:
                orderedHives = hives.OrderByDescending(h => h.CreatedAt);
                break;
            case HiveOrderBy.Old:
                orderedHives = hives.OrderBy(h => h.CreatedAt);
                break;
            case HiveOrderBy.Users:
                orderedHives = hives.OrderByDescending(h => h.UserCount);
                break;
            default:
                throw new ValidationException("Invalid order by option.");
        }

        return orderedHives.ThenBy(h => h.Id);
    }

    
    public static IQueryable<EntityWithCursorDto<Hive>> ToEntityWithCursorDto(this IQueryable<Hive> entities, HivePaginationDto pagination)
    {
        return pagination.OrderBy switch
        {
            HiveOrderBy.New => entities.Select(c => new EntityWithCursorDto<Hive> { Entity = c, Rank = c.CreatedAt }),
            HiveOrderBy.Old => entities.Select(c => new EntityWithCursorDto<Hive> { Entity = c, Rank = c.CreatedAt }),
            HiveOrderBy.Users => entities.Select(c => new EntityWithCursorDto<Hive> { Entity = c, Rank = c.UserCount }),
            _ => throw new ValidationException("Invalid order by option.")
        };
    }
}