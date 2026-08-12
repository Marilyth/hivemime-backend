/// <summary>
/// Represents a single vote query, e.g. CandidateGuid = 4.
/// </summary>
public class FilterQuery : FilterQueryBase
{
    public string Property { get; set; }
    public SubProperty? SubProperty { get; set; }
    public ValueOperator ValueOperator { get; set; }
    public string Value { get; set; }

    protected override string GetQueryExpression()
        => $"{Property}.{SubProperty} {ValueOperator.OperatorToSymbol()} {Value}";

    public CandidateType GetCandidateType()
    {
        return Property switch
        {
            ":Country" => CandidateType.Country,
            ":Age" => CandidateType.Age,
            _ => CandidateType.Regular
        };
    }


    public enum CandidateType
    {
        Regular,
        Country,
        Age
    }
}