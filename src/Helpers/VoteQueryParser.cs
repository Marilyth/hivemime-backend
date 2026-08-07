using System.Linq.Expressions;

public static class VoteQueryParser
{
    /// <summary>
    /// Parses a query expression string into a VoteQueryBase object.
    /// </summary>
    /// <param name="queryExpression">The query expression string to parse.</param>
    /// <returns>The root of the query tree represented by the query expression string.</returns>
    public static FilterQueryBase ToVoteQuery(this string queryExpression)
    {
        // TODO 9: Implement a parser that converts the query expression string into a VoteQueryBase object.

        return null;
    }

    /// <summary>
    /// Converts a VoteQueryBase object into an expression that can be used to filter PostVotes in EFCore.
    /// </summary>
    /// <param name="query">The query to convert.</param>
    /// <returns>The expression representing the query.</returns>
    public static Expression<Func<PostVote, bool>> ToExpression(this FilterQueryBase query)
    {
        // This method can be used if the query already has identifiers instead of indices.
        // It can be used for testing purposes or if the caller has already converted indices to identifiers.

        // TODO 9: Convert the query to an expression for EFCore.

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