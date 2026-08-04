using System.Text;

/// <summary>
/// Represents a group of queries.
/// </summary>
public class FilterQueryGroup : FilterQueryBase
{
    public List<FilterQueryBase> Children { get; set; }

    protected override string GetQueryExpression()
    {
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < Children.Count; i++)
        {
            if (i > 0)
                sb.Append($" {Children[i].LeftOperator.ToString().ToUpper()} ");
                
            sb.Append(Children[i].ToString());
        }

        return $"({sb})";
    }
}
