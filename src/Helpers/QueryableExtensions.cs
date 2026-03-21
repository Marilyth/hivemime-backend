using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

public static class QueryableExtensions
{
    /// <summary>
    /// Returns the first element of a sequence that satisfies a specified condition or throws an exception if no such element is found.
    /// The exception message includes the type of the entity that was not found.
    /// </summary>
    /// <param name="query">The source IQueryable to return an element from.</param>
    /// <param name="predicate">The condition to test against the elements of the sequence.</param>
    public static async Task<T> FirstOrExceptionAsync<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
    {
        return await query.FirstOrDefaultAsync(predicate) ?? 
            throw new Exception($"The requested {typeof(T).Name} was not found.");
    }
}