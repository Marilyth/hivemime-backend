using System.Linq.Expressions;

public static class QueryableExtensions
{
    public static T FirstOrException<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
    {
        return query.FirstOrDefault(predicate) ?? 
            throw new Exception($"The requested {typeof(T).Name} was not found.");
    }
}