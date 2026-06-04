public class UserPaginationHelper : PaginationHelperBase<User, UserPaginationDto>
{
    public UserPaginationHelper(UserPaginationDto pagination) : base(pagination)
    {
        IsDescending = pagination.OrderBy is UserOrderBy.New or UserOrderBy.Honey;

        PropertySelector = pagination.OrderBy switch
        {
            UserOrderBy.New => (User u) => u.CreatedAt,
            UserOrderBy.Old => (User u) => u.CreatedAt,
            UserOrderBy.Honey => (User u) => u.Honey,
            UserOrderBy.Name => (User u) => u.Username,
            _ => throw new ValidationException("Invalid order by option.")
        };

        Cursor = Pagination.Cursor is null ? null : pagination.OrderBy switch
        {
            UserOrderBy.New => DateTimeOffset.Parse(Pagination.Cursor.Cursor),
            UserOrderBy.Old => DateTimeOffset.Parse(Pagination.Cursor.Cursor),
            UserOrderBy.Honey => int.Parse(Pagination.Cursor.Cursor),
            UserOrderBy.Name => Pagination.Cursor.Cursor,
            _ => throw new ValidationException("Invalid order by option.")
        };
    }

    protected override IQueryable<User> ApplyPreFiltering(IQueryable<User> query)
    {
        if (Pagination.Filter is not null)
        {
            string filter = Pagination.Filter.Trim().ToLower();
            query = query.Where(u => u.Username.ToLower().StartsWith(filter));
        }

        return query;
    }
}