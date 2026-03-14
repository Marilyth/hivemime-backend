/// <summary>
/// Represents a single vote query, e.g. 1:1=4.
/// </summary>
public class VoteQuery : VoteQueryBase
{
    public int PollIndex { get; set; }
    public int CandidateIndex { get; set; }
    public ValueOperator ValueOperator { get; set; }
    public object Value { get; set; }

    protected override string GetQueryExpression()
        => $"{PollIndex}:{CandidateIndex}{ValueOperator.OperatorToSymbol()}{Value}";
}