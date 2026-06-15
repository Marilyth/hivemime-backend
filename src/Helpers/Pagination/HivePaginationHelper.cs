using Microsoft.EntityFrameworkCore;

public class HivePaginationHelper : PaginationHelperBase<Hive, HivePaginationDto>
{
    public HivePaginationHelper(HivePaginationDto pagination) : base(pagination)
    {
        IsDescending = pagination.OrderBy is HiveOrderBy.New or HiveOrderBy.Users;

        PropertySelector = Pagination.OrderBy switch
        {
            HiveOrderBy.New => (Hive h) => h.CreatedAt,
            HiveOrderBy.Old => (Hive h) => h.CreatedAt,
            HiveOrderBy.Users => (Hive h) => h.UserCount,
            _ => throw new ValidationException("Invalid order by option.")
        };

        Cursor = Pagination.Cursor is null ? null : Pagination.OrderBy switch
        {
            HiveOrderBy.New => DateTimeOffset.Parse(Pagination.Cursor.Cursor),
            HiveOrderBy.Old => DateTimeOffset.Parse(Pagination.Cursor.Cursor),
            HiveOrderBy.Users => int.Parse(Pagination.Cursor.Cursor),
            _ => throw new ValidationException("Invalid order by option.")
        };
    }

    protected override IQueryable<Hive> ApplyPreFiltering(IQueryable<Hive> query)
    {
        if (!string.IsNullOrWhiteSpace(Pagination.Filter))
        {
            string filter = Pagination.Filter.Trim();
            query = query.Where(h => h.Name.StartsWith(filter) ||
                                     h.SearchVector.Matches(EF.Functions.WebSearchToTsQuery("simple", filter)));
        }

        return query;
    }
}