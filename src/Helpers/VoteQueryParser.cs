using System.Collections;
using System.Linq.Expressions;

public static class VoteQueryParser
{
    /// <summary>
    /// Returns all leaf nodes of a VoteQueryBase object.
    /// </summary>
    /// <param name="query">The query to get the leaves from.</param>
    /// <returns>An enumerable of all leaf nodes.</returns>
    public static IEnumerable<FilterQuery> GetLeaves(this FilterQueryBase query)
    {
        if (query is FilterQuery leaf)
            yield return leaf;

        else if (query is FilterQueryGroup group)
            foreach (FilterQueryBase child in group.Children)
                foreach (FilterQuery childLeaf in child.GetLeaves())
                    yield return childLeaf;
    }

    /// <summary>
    /// Converts a VoteQueryBase object into an expression that can be used to filter PostVotes in EFCore.
    /// </summary>
    /// <param name="query">The query to convert.</param>
    /// <returns>The expression representing the query.</returns>
    public static Expression<Func<PostVote, bool>> ToExpression(this FilterQueryBase query)
    {
        if (query is null)
            return null;

        Expression<Func<PostVote, bool>> currentExpression;

        switch (query)
        {
            case FilterQuery leaf:
                currentExpression = ToExpression(leaf.Property, leaf.SubProperty, leaf.ValueOperator, leaf.Value);
                break;
            case FilterQueryGroup group:
                FilterQueryBase left = group.Children[0];
                FilterQueryBase right = group.Children[1];

                Expression<Func<PostVote, bool>> leftExpression = left.ToExpression();
                Expression<Func<PostVote, bool>> rightExpression = right.ToExpression();

                Expression body = right.LeftOperator switch
                {
                    BooleanOperator.And => Expression.AndAlso(leftExpression.Body, rightExpression.Body),
                    BooleanOperator.Or => Expression.OrElse(leftExpression.Body, rightExpression.Body),
                    _ => throw new ArgumentException("Invalid boolean operator.")
                };

                currentExpression = Expression.Lambda<Func<PostVote, bool>>(body, leftExpression.Parameters);
                break;
            default:
                throw new ArgumentException("Invalid query type.");
        }

        if (query.IsNegated)
            currentExpression = Expression.Lambda<Func<PostVote, bool>>(Expression.Not(currentExpression.Body), currentExpression.Parameters);

        return currentExpression;
    }

    private static Expression<Func<PostVote, bool>> ToExpression(string property, SubProperty? subProperty, ValueOperator op, string value)
    {
        // ToDo 9: Implement query leaf to expression.
        // Keep in mind this must use .Any across all candidates matching the property, since multiple votes can be cast for a single candidate.
        return p => true;
    }

    /// <summary>
    /// Cleans up a query by removing redundant groups and negations in-place.
    /// </summary>
    /// <param name="query">The query to clean up.</param>
    public static FilterQueryBase? CleanUp(this FilterQueryBase query)
    {
        if (query is FilterQuery || query is null)
            return query;

        FilterQueryGroup group = query as FilterQueryGroup;

        for(int i = group.Children!.Count - 1; i >= 0; i--)
        {
            group.Children[i] = group.Children[i].CleanUp();

            if (group.Children[i] is null)
                group.Children.RemoveAt(i);
        }

        if (group.Children.Count == 1)
        {
            group.Children[0].IsNegated = group.IsNegated != group.Children[0].IsNegated;
            return group.Children[0];
        }
        else if (group.Children.Count == 0)
            return null;

        return group;
    }

    /// <summary>
    /// Converts a query to a greedy balanced abstract syntax tree representation in-place.
    /// </summary>
    /// <param name="query">The query to convert.</param>
    public static FilterQueryBase ToAST(this FilterQueryBase query)
    {
        query = query.CleanUp();

        return query switch
        {
            null => null,
            FilterQuery leaf => leaf,
            FilterQueryGroup group => Split(group),
            _ => throw new ValidationException("Invalid query type.")
        };
    }

    private static FilterQueryBase Split(FilterQueryGroup group)
    {
        var children = group.Children!;
        var middleIndex = children.Count / 2;
        var splitIndex = -1;

        for (var i = 1; i < children.Count && Math.Abs(i - middleIndex) <= Math.Abs(splitIndex - middleIndex); i++)
        {
            if (children[i].LeftOperator == BooleanOperator.Or)
                splitIndex = i;
        }

        if (splitIndex == -1)
            splitIndex = middleIndex;

        var leftGroup = new FilterQueryGroup() { Children = children[..splitIndex] };
        var rightGroup = new FilterQueryGroup() { Children = children[splitIndex..], LeftOperator = children[splitIndex].LeftOperator };

        var resultChildren = new List<FilterQueryBase>()
        {
            leftGroup.ToAST(),
            rightGroup.ToAST()
        };

        group.Children = resultChildren;

        return group;
    }
}