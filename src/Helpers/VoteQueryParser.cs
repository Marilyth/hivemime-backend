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
    /// Converts a query to a greedy balanced abstract syntax tree representation.
    /// </summary>
    /// <param name="query">The query to convert.</param>
    public static FilterQueryBase ToAST(this FilterQueryBase query)
    {
        return query switch
        {
            null => throw new ValidationException("A query cannot be null."),
            FilterQueryGroup group when group.Children!.Count == 0 => throw new ValidationException("A query without children is invalid."),
            FilterQuery leaf => new FilterQuery()
            {
                IsNegated = leaf.IsNegated,
                LeftOperator = leaf.LeftOperator,
                Property = leaf.Property,
                Value = leaf.Value,
                ValueOperator = leaf.ValueOperator
            },
            FilterQueryGroup group when group.Children!.Count == 1 => Collapse(group.Children[0], group.IsNegated),
            FilterQueryGroup group => Split(group),
            _ => throw new ValidationException("Invalid query type.")
        };
    }

    private static FilterQueryBase Collapse(FilterQueryBase child, bool negate)
    {
        var result = child.ToAST();
        result.IsNegated = negate != result.IsNegated;
        
        return result;
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

        return new FilterQueryGroup()
        {
            Children = resultChildren,
            LeftOperator = group.LeftOperator,
            IsNegated = group.IsNegated
        };
    }
}