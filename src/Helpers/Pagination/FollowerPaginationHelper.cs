using Mapster;

public static class HiveUserPaginationHelper
{
    public static IQueryable<HiveUser> ApplyPaginationFilter(this IQueryable<HiveUser> hiveUsers, HiveUserPaginationDto pagination)
    {
        if (pagination.Filter is not null)
        {
            string filter = pagination.Filter.Trim().ToLower();
            hiveUsers = hiveUsers.Where(f => f.User.Username.ToLower().StartsWith(filter));
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
        IOrderedQueryable<HiveUser> orderedHiveUsers;

        switch (pagination.OrderBy)
        {
            case HiveUserOrderBy.New:
                orderedHiveUsers = hiveUsers.OrderByDescending(f => f.CreatedAt);
                break;
            case HiveUserOrderBy.Old:
                orderedHiveUsers = hiveUsers.OrderBy(f => f.CreatedAt);
                break;
            default:
                throw new ValidationException("Invalid order by option.");
        }

        return orderedHiveUsers.ThenBy(f => f.Id);
    }

    public static async Task<PaginationResultDto<HiveUserDto>> FetchPaginationResultAsync(this IQueryable<HiveUser> entities, HiveUserPaginationDto pagination)
    {
        return await PostPaginationHelper.BuildPaginationResultAsync(entities.ProjectToType<HiveUserDto>(), pagination, f => pagination.OrderBy switch
        {
            HiveUserOrderBy.New or HiveUserOrderBy.Old => f.CreatedAt,
            _ => throw new ValidationException("Invalid order by option.")
        });
    }
}