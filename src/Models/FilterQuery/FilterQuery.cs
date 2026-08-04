/// <summary>
/// Represents a single vote query, e.g. 1:1=4.
/// </summary>
public class FilterQuery : FilterQueryBase
{
    public string Property { get; set; }
    public ValueOperator ValueOperator { get; set; }
    public string Value { get; set; }

    protected override string GetQueryExpression()
        => $"{Property}{ValueOperator.OperatorToSymbol()}{Value}";

    public CandidateType GetCandidateType()
    {
        return Property switch
        {
            ":Country" => CandidateType.Country,
            ":Age" => CandidateType.Age,
            ":Date" => CandidateType.Date,
            _ => CandidateType.Regular
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