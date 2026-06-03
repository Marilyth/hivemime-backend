using Mapster;

public static class HiveUserPaginationHelper
{
    public static IQueryable<HiveUser> ApplyPaginationFilter(this IQueryable<HiveUser> hiveUsers, HiveUserPaginationDto pagination)
    {
        if (pagination.Filter is not null)
        {
            string filter = pagination.Filter.Trim();
            hiveUsers = hiveUsers.Where(f => f.User.Username.StartsWith(filter));
        }

        if (pagination.Cursor is null)
            return hiveUsers;

        switch (pagination.OrderBy)
        {
            case HiveUserOrderBy.New:
                DateTimeOffset newCursor = DateTimeOffset.Parse(pagination.Cursor.Cursor);
                return hiveUsers.Where(f => f.CreatedAt < newCursor ||
                                       (f.CreatedAt == newCursor && f.Id > pagination.Cursor.Id));
            case HiveUserOrderBy.Old:
                DateTimeOffset oldCursor = DateTimeOffset.Parse(pagination.Cursor.Cursor);
                return hiveUsers.Where(f => f.CreatedAt > oldCursor ||
                                       (f.CreatedAt == oldCursor && f.Id > pagination.Cursor.Id));
            default:
                throw new ValidationException("Invalid order by option.");
        }
    }

    public static IOrderedQueryable<HiveUser> ApplyPaginationOrdering(this IQueryable<HiveUser> hiveUsers, HiveUserPaginationDto pagination)
    {
        IOrderedQueryable<HiveUser> orderedHiveUsers = hiveUsers.OrderByDescending(f => f.Role);

        switch (pagination.OrderBy)
        {
            case HiveUserOrderBy.New:
                orderedHiveUsers = orderedHiveUsers.ThenByDescending(f => f.CreatedAt);
                break;
            case HiveUserOrderBy.Old:
                orderedHiveUsers = orderedHiveUsers.ThenBy(f => f.CreatedAt);
                break;
            default:
                throw new ValidationException("Invalid order by option.");
        }

        return orderedHiveUsers.ThenBy(f => f.Id);
    }

    public static IQueryable<EntityWithCursorDto<HiveUser>> ToEntityWithCursorDto(this IQueryable<HiveUser> entities, HiveUserPaginationDto pagination)
    {
        return pagination.OrderBy switch
        {
            HiveUserOrderBy.New => entities.Select(c => new EntityWithCursorDto<HiveUser> { Entity = c, Rank = c.CreatedAt }),
            HiveUserOrderBy.Old => entities.Select(c => new EntityWithCursorDto<HiveUser> { Entity = c, Rank = c.CreatedAt }),
            _ => throw new ValidationException("Invalid order by option.")
        };
    }
}