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
    public static async Task<T> FirstOrExceptionAsync<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate = null)
    {
        return await (predicate != null ? query.FirstOrDefaultAsync(predicate) : query.FirstOrDefaultAsync()) ?? 
            throw new NotFoundException($"The requested {typeof(T).Name} was not found.");
    }

    /// <summary>
    /// Filters the elements of an IQueryable based on a specified key value. The key is compared to the Id property of the entities in the query.
    /// </summary>
    /// <typeparam name="T">The type of the entities in the IQueryable, which must inherit from EntityWithIdentifier.</typeparam>
    /// <param name="query">The source IQueryable to filter.</param>
    /// <param name="key">The value to filter the entities by, compared against their Id property.</param>
    public static IQueryable<T> QueryableFind<T>(this IQueryable<T> query, object key) where T : EntityWithIdentifier
        => query.Where(e => e.Id.Equals(key));

    /// <summary>
    /// Returns an IQueryable that contains only the entity with the specified Id. The entity type must inherit from EntityWithIdentifier.
    /// </summary> <typeparam name="T">The type of the entity, which must inherit from EntityWithIdentifier.</typeparam>
    /// <param name="entity">The entity to find, whose Id will be used as the key for filtering.</param>
    /// <param name="context">The HiveMimeContext to access the DbSet for the entity type.</param>
    public static IQueryable<T> ToQueryable<T>(this T entity, HiveMimeContext context) where T : EntityWithIdentifier
        => context.Set<T>().QueryableFind(entity.Id);
}