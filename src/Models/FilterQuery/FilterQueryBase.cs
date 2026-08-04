/// <summary>
/// Base class for all queries, representing either a single query or a group of queries.
/// </summary>
public abstract class FilterQueryBase
{
    public bool IsNegated { get; set; }
    public BooleanOperator LeftOperator { get; set; }

    public override string ToString()
    {
        string queryExpression = GetQueryExpression();

        if (IsNegated)
            queryExpression = $"NOT {queryExpression}";

        return queryExpression;
    }

    protected abstract string GetQueryExpression();
}
