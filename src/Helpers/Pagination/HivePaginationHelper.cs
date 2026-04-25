public static class HivePaginationHelper
{
    public static IQueryable<Hive> ApplyPaginationFilter(this IQueryable<Hive> hives, HivePaginationDto pagination)
    {
        // TODO 5: Add reverse index. This does not scale well.
        if (pagination.Filter is not null)
        {
            string filter = pagination.Filter.Trim().ToLower();
            hives = hives.Where(h => h.Name.ToLower().Contains(filter));
        }
        
        if (pagination.Cursor is null)
            return hives;

        var cursor = hives.QueryableFind(pagination.Cursor);
        var intermediateQuery = hives.SelectMany(h => cursor.DefaultIfEmpty(), (h, cursor) => new { Current = h, Cursor = cursor });

        switch (pagination.OrderBy)
        {
            // ToDo: Implement hive scoring.
            case HiveOrderBy.New:
                return intermediateQuery.Where(h => h.Current.CreatedAt < h.Cursor.CreatedAt ||
                                                   (h.Current.CreatedAt == h.Cursor.CreatedAt && h.Current.Id > h.Cursor.Id))
                    .Select(h => h.Current);
            case HiveOrderBy.Old:
                return intermediateQuery.Where(h => h.Current.CreatedAt > h.Cursor.CreatedAt ||
                                                   (h.Current.CreatedAt == h.Cursor.CreatedAt && h.Current.Id > h.Cursor.Id))
                    .Select(h => h.Current);
            case HiveOrderBy.Followers:
                return intermediateQuery.Where(h => h.Current.FollowerCount < h.Cursor.FollowerCount ||
                                                   (h.Current.FollowerCount == h.Cursor.FollowerCount && h.Current.Id > h.Cursor.Id))
                    .Select(h => h.Current);
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
            case HiveOrderBy.Followers:
                orderedHives = hives.OrderByDescending(h => h.FollowerCount);
                break;
            default:
                throw new ValidationException("Invalid order by option.");
        }

        return orderedHives.ThenBy(h => h.Id);
    }

    public static IQueryable<Hive> ApplyPaginationPageSize(this IQueryable<Hive> hives, HivePaginationDto pagination, int maxPageSize = 100)
    {
        pagination.PageSize = Math.Clamp(pagination.PageSize, 1, maxPageSize);
        
        return hives.Take(pagination.PageSize);
    }
}