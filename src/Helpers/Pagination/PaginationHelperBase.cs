using System.Linq.Expressions;
using Mapster;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Provides cursor-based pagination over an <see cref="IQueryable{TEntity}"/> using expression-tree-based
/// ordering, filtering, and projection.
/// 
/// This implementation relies heavily on expression tree rewriting because the ordering property
/// is only known at runtime.
/// </summary>
/// <typeparam name="TEntity">Entity type being paginated.</typeparam>
/// <typeparam name="TPaginationDto">Input pagination request DTO.</typeparam>
public abstract class PaginationHelperBase<TEntity, TPaginationDto>
    where TEntity : EntityWithIdentifier
    where TPaginationDto : PaginationDto
{
    /// <summary>
    /// Cached rewritten selector where the original PropertySelector parameter
    /// has been replaced with this class's shared <see cref="Parameter"/>.
    /// </summary>
    private LambdaExpression _replacedPropertySelector;

    public PaginationHelperBase(TPaginationDto pagination)
    {
        Pagination = pagination;
    }

    protected TPaginationDto Pagination { get; }

    /// <summary>
    /// Maximum allowed page size (hard cap applied in <see cref="ApplyPaginationPageSize"/>).
    /// </summary>
    protected int MaxPageSize { get; set; } = 100;

    /// <summary>
    /// Determines sort direction for cursor pagination.
    /// </summary>
    protected bool IsDescending { get; set; }

    /// <summary>
    /// Cursor value used for filtering. Must match the type of <see cref="PropertySelector"/>.
    /// </summary>
    protected object? Cursor { get; set; }

    /// <summary>
    /// Expression describing the property used for ordering and cursor comparison.
    /// Must be a simple member access expression (e.g. e => e.CreatedAt).
    /// </summary>
    protected LambdaExpression PropertySelector { get; set; }

    /// <summary>
    /// Shared parameter used across all dynamically built expression trees.
    /// 
    /// IMPORTANT: Expression trees require reference equality for parameters.
    /// All rewritten expressions MUST use this instance.
    /// </summary>
    private ParameterExpression Parameter { get; }
        = Expression.Parameter(typeof(TEntity), "e");

    public virtual async Task<PaginationResultDto<TDto>> ApplyPaginationAsync<TDto>(IQueryable<TEntity> query)
        where TDto : IHasIdentifier
    {
        query = ApplyPreFiltering(query);
        query = ApplyPaginationFilter(query);
        query = ApplyPaginationOrdering(query);
        query = ApplyPaginationPageSize(query);

        IQueryable<EntityWithCursorDto<TEntity>> queryWithCursor = ToEntityWithCursorDto(query);

        return await BuildPaginationResultAsync<TDto>(queryWithCursor);
    }

    /// <summary>
    /// Optional hook for applying additional filters before pagination logic runs.
    /// </summary>
    protected virtual IQueryable<TEntity> ApplyPreFiltering(IQueryable<TEntity> query)
    {
        return query;
    }

    /// <summary>
    /// Applies cursor-based filtering:
    /// returns items before/after cursor depending on sort direction,
    /// and uses Id as a deterministic tie-breaker.
    /// </summary>
    private IQueryable<TEntity> ApplyPaginationFilter(IQueryable<TEntity> query)
    {
        if (Pagination.Cursor is null)
            return query;

        Expression cursorExpression = Expression.Constant(Cursor);
        Expression propertyExpression = GetReplacedPropertySelector().Body;

        Expression filterExpression =
            Expression.OrElse(
                IsDescending
                    ? Expression.LessThan(propertyExpression, cursorExpression)
                    : Expression.GreaterThan(propertyExpression, cursorExpression),
                Expression.AndAlso(
                    Expression.Equal(propertyExpression, cursorExpression),
                    Expression.GreaterThan(
                        Expression.Property(Parameter, nameof(EntityWithIdentifier.Id)),
                        Expression.Constant(Pagination.Cursor.Id))
                ));

        Expression<Func<TEntity, bool>> lambda =
            Expression.Lambda<Func<TEntity, bool>>(filterExpression, Parameter);

        return query.Where(lambda);
    }

    /// <summary>
    /// Applies dynamic OrderBy / OrderByDescending using expression-tree construction.
    /// Then applies deterministic secondary ordering by Id.
    /// 
    /// NOTE: Queryable.OrderBy cannot be used directly because the key type is unknown at compile time.
    /// </summary>
    private IQueryable<TEntity> ApplyPaginationOrdering(IQueryable<TEntity> query)
    {
        IOrderedQueryable<TEntity> orderedQuery =
            (IOrderedQueryable<TEntity>)query.Provider.CreateQuery<TEntity>(
                Expression.Call(
                    typeof(Queryable),
                    IsDescending ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy),
                    new[] { typeof(TEntity), PropertySelector.Body.Type },
                    query.Expression,
                    Expression.Quote(PropertySelector)));

        return orderedQuery.ThenBy(e => e.Id);
    }

    /// <summary>
    /// Enforces page size limits and fetches one extra record
    /// to determine if a next page exists.
    /// </summary>
    private IQueryable<TEntity> ApplyPaginationPageSize(IQueryable<TEntity> query)
    {
        Pagination.PageSize = Math.Clamp(Pagination.PageSize, 1, MaxPageSize);
        return query.Take(Pagination.PageSize + 1);
    }

    /// <summary>
    /// Projects entity query into cursor-aware DTO form.
    /// Rank is stored as object because it is used for cursor serialization,
    /// not SQL computation.
    /// </summary>
    private IQueryable<EntityWithCursorDto<TEntity>> ToEntityWithCursorDto(IQueryable<TEntity> query)
    {
        var body = Expression.MemberInit(
            Expression.New(typeof(EntityWithCursorDto<TEntity>)),
            Expression.Bind(
                typeof(EntityWithCursorDto<TEntity>).GetProperty(nameof(EntityWithCursorDto<>.Entity))!,
                Parameter),
            Expression.Bind(
                typeof(EntityWithCursorDto<TEntity>).GetProperty(nameof(EntityWithCursorDto<>.Rank))!,
                Expression.Convert(GetReplacedPropertySelector().Body, typeof(object))
            )
        );

        var selector =
            Expression.Lambda<Func<TEntity, EntityWithCursorDto<TEntity>>>(body, Parameter);

        return query.Select(selector);
    }

    /// <summary>
    /// Executes query and builds final paginated result set with cursor.
    /// </summary>
    private async Task<PaginationResultDto<TDto>> BuildPaginationResultAsync<TDto>(
        IQueryable<EntityWithCursorDto<TEntity>> query)
        where TDto : IHasIdentifier
    {
        var result = await query
            .ProjectToType<EntityWithCursorDto<TDto>>()
            .ToListAsync();

        EntityWithCursorDto<TDto>? lastItem =
            result.Count > Pagination.PageSize
                ? result[Pagination.PageSize - 1]
                : default;

        PaginationCursorDto cursor =
            lastItem is null
                ? null
                : new()
                {
                    Cursor = lastItem.Rank.ToString(),
                    Id = lastItem.Entity.Id
                };

        return new PaginationResultDto<TDto>
        {
            Items = result.Take(Pagination.PageSize).Select(e => e.Entity).ToList(),
            NextCursor = cursor
        };
    }

    /// <summary>
    /// Rewrites PropertySelector so it uses the shared Parameter instance.
    /// This is required because expression trees are identity-based, not name-based.
    /// </summary>
    private LambdaExpression GetReplacedPropertySelector()
    {
        if (_replacedPropertySelector != null)
            return _replacedPropertySelector;

        Expression newBody =
            new ReplaceParameterVisitor(PropertySelector.Parameters[0], Parameter)
                .Visit(PropertySelector.Body);

        _replacedPropertySelector =
            Expression.Lambda(newBody, Parameter);

        return _replacedPropertySelector;
    }

    /// <summary>
    /// Internal DTO used to carry entity + cursor value through the query pipeline.
    /// </summary>
    private class EntityWithCursorDto<T> where T : IHasIdentifier
    {
        public T Entity { get; set; }
        public object Rank { get; set; }
    }

    /// <summary>
    /// Replaces parameter references inside an expression tree.
    /// Required because expression trees compare parameters by reference, not by name.
    /// </summary>
    private class ReplaceParameterVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParam;
        private readonly ParameterExpression _newParam;

        public ReplaceParameterVisitor(ParameterExpression oldParam, ParameterExpression newParam)
        {
            _oldParam = oldParam;
            _newParam = newParam;
        }

        protected override Expression VisitParameter(ParameterExpression node)
            => node == _oldParam ? _newParam : base.VisitParameter(node);
    }
}