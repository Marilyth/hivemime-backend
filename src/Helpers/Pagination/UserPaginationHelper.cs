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

    public static IQueryable<EntityWithCursorDto<User>> ToEntityWithCursorDto(this IQueryable<User> entities, UserPaginationDto pagination)
    {
        return pagination.OrderBy switch
        {
            UserOrderBy.New => entities.Select(c => new EntityWithCursorDto<User> { Entity = c, Rank = c.CreatedAt }),
            UserOrderBy.Old => entities.Select(c => new EntityWithCursorDto<User> { Entity = c, Rank = c.CreatedAt }),
            UserOrderBy.Honey => entities.Select(c => new EntityWithCursorDto<User> { Entity = c, Rank = c.Honey }),
            UserOrderBy.Name => entities.Select(c => new EntityWithCursorDto<User> { Entity = c, Rank = c.Username }),
            _ => throw new ValidationException("Invalid order by option.")
        };
    }
}