using Mapster;

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
            case HiveOrderBy.Followers:
                int followersCursor = int.Parse(pagination.Cursor.Cursor);
                return hives.Where(h => h.FollowerCount < followersCursor ||
                                       (h.FollowerCount == followersCursor && h.Id > pagination.Cursor.Id));
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


    public static async Task<PaginationResultDto<HiveDto>> FetchPaginationResultAsync(this IQueryable<Hive> entities, HivePaginationDto pagination)
    {
        return await PostPaginationHelper.BuildPaginationResultAsync(entities.ProjectToType<HiveDto>(), pagination, h => pagination.OrderBy switch
        {
            HiveOrderBy.New or HiveOrderBy.Old => h.CreatedAt,
            HiveOrderBy.Followers => h.FollowerCount,
            _ => throw new ValidationException("Invalid order by option.")
        });
    }
}