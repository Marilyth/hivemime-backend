public class HiveUserPaginationHelper : PaginationHelperBase<HiveUser, HiveUserPaginationDto>
{
    public HiveUserPaginationHelper(HiveUserPaginationDto pagination) : base(pagination)
    {
        IsDescending = pagination.OrderBy is HiveUserOrderBy.New;

        PropertySelector = pagination.OrderBy switch
        {
            HiveUserOrderBy.New => (HiveUser f) => f.CreatedAt,
            HiveUserOrderBy.Old => (HiveUser f) => f.CreatedAt,
            _ => throw new ValidationException("Invalid order by option.")
        };

        Cursor = Pagination.Cursor is null ? null : pagination.OrderBy switch
        {
            HiveUserOrderBy.New => DateTimeOffset.Parse(Pagination.Cursor.Cursor),
            HiveUserOrderBy.Old => DateTimeOffset.Parse(Pagination.Cursor.Cursor),
            _ => throw new ValidationException("Invalid order by option.")
        };
    }

    protected override IQueryable<HiveUser> ApplyPreFiltering(IQueryable<HiveUser> query)
    {
        if (!string.IsNullOrWhiteSpace(Pagination.Filter))
        {
            string filter = Pagination.Filter.Trim();
            query = query.Where(f => f.User.Username.StartsWith(filter));
        }

        return query;
    }
}