using Mapster;

public static class UserPaginationHelper
{
    public static IQueryable<User> ApplyPaginationFilter(this IQueryable<User> users, UserPaginationDto pagination)
    {
        if (pagination.Filter is not null)
        {
            string filter = pagination.Filter.Trim().ToLower();
            users = users.Where(u => u.Username.ToLower().StartsWith(filter));
        }

        if (pagination.Cursor is null)
            return users;

        switch (pagination.OrderBy)
        {
            case UserOrderBy.New:
                DateTimeOffset newCursor = DateTimeOffset.Parse(pagination.Cursor.Cursor);
                return users.Where(u => u.CreatedAt < newCursor ||
                                       (u.CreatedAt == newCursor && u.Id > pagination.Cursor.Id));
            case UserOrderBy.Old:
                DateTimeOffset oldCursor = DateTimeOffset.Parse(pagination.Cursor.Cursor);
                return users.Where(u => u.CreatedAt > oldCursor ||
                                       (u.CreatedAt == oldCursor && u.Id > pagination.Cursor.Id));
            case UserOrderBy.Honey:
                int honeyCountCursor = int.Parse(pagination.Cursor.Cursor);
                return users.Where(u => u.Honey < honeyCountCursor ||
                                       (u.Honey == honeyCountCursor && u.Id > pagination.Cursor.Id));
            case UserOrderBy.Name:
                string nameCursor = pagination.Cursor.Cursor;
                return users.Where(u => string.Compare(u.Username, nameCursor) > 0 ||
                                       (u.Username == nameCursor && u.Id > pagination.Cursor.Id));
            default:
                throw new ValidationException("Invalid order by option.");
        }
    }

    public static IOrderedQueryable<User> ApplyPaginationOrdering(this IQueryable<User> users, UserPaginationDto pagination)
    {
        IOrderedQueryable<User> orderedUsers;

        switch (pagination.OrderBy)
        {
            case UserOrderBy.New:
                orderedUsers = users.OrderByDescending(u => u.CreatedAt);
                break;
            case UserOrderBy.Old:
                orderedUsers = users.OrderBy(u => u.CreatedAt);
                break;
            case UserOrderBy.Honey:
                orderedUsers = users.OrderByDescending(u => u.Honey);
                break;
            case UserOrderBy.Name:
                orderedUsers = users.OrderBy(u => u.Username);
                break;
            default:
                throw new ValidationException("Invalid order by option.");
        }

        return orderedUsers.ThenBy(u => u.Id);
    }

    public static async Task<PaginationResultDto<UserDto>> FetchPaginationResultAsync(this IQueryable<User> entities, UserPaginationDto pagination)
    {
        return await PostPaginationHelper.BuildPaginationResultAsync(entities.ProjectToType<UserDto>(), pagination, u => pagination.OrderBy switch
        {
            UserOrderBy.New or UserOrderBy.Old => u.CreatedAt,
            UserOrderBy.Honey => u.Honey,
            UserOrderBy.Name => u.Username,
            _ => throw new ValidationException("Invalid order by option.")
        });
    }
}