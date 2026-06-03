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
                                     h.SearchVector.Matches(EF.Functions.WebSearchToTsQuery("english", filter)));
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


    public static async Task<PaginationResultDto<HiveDto>> FetchPaginationResultAsync(this IQueryable<Hive> entities, HivePaginationDto pagination)
    {
        return await PostPaginationHelper.BuildPaginationResultAsync(entities.ProjectToType<HiveDto>(), pagination, h => pagination.OrderBy switch
        {
            HiveOrderBy.New or HiveOrderBy.Old => h.CreatedAt,
            HiveOrderBy.Users => h.UserCount,
            _ => throw new ValidationException("Invalid order by option.")
        });
    }
}