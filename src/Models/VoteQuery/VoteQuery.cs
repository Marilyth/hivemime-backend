/// <summary>
/// Represents a single vote query, e.g. 1:1=4.
/// </summary>
public class VoteQuery : VoteQueryBase
{
    public int CandidateId { get; set; }
    public ValueOperator ValueOperator { get; set; }
    public object Value { get; set; }

    protected override string GetQueryExpression()
        => $"{CandidateId}{ValueOperator.OperatorToSymbol()}{Value}";
}