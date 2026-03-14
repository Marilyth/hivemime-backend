/// <summary>
/// Represents a single vote query, e.g. 1:1=4.
/// </summary>
public class VoteQuery : VoteQueryBase
{
    /// <summary>
    /// The Id of the candidate this query is filtering.
    /// Candidates of pseudo-polls have negative Ids as specified in <seealso cref="GetCandidateType"/>.
    /// Those must be filtered explicitly through the PostVote or User entities instead of through the CandidateVote.
    /// </summary>
    public int CandidateId { get; set; }
    public ValueOperator ValueOperator { get; set; }
    public object Value { get; set; }

    protected override string GetQueryExpression()
        => $"{CandidateId}{ValueOperator.OperatorToSymbol()}{Value}";

    public CandidateType GetCandidateType()
    {
        return CandidateId switch
        {
            > 0 => CandidateType.Regular,
            -1 => CandidateType.Country,
            -3 => CandidateType.Date,
            -2 => CandidateType.Age,
            _ => throw new InvalidOperationException($"Invalid CandidateId {CandidateId}.")
        };
    }


    public enum CandidateType
    {
        Regular,
        Country,
        Date,
        Age
    }
}