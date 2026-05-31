public class PollResultDto<T> : PollDto
{
    public new List<T> Candidates { get; set; }
}

public class CandidateSumResultDto
{
    public Guid Id { get; set; }
    public double TotalScore { get; set; }
}

public class CandidateStatisticsResultDto
{
    public Guid Id { get; set; }
    public double Min { get; set; }
    public double Q1 { get; set; }
    public double Median { get; set; }
    public double Q3 { get; set; }
    public double Max { get; set; }
}

public class CandidateDistributionResultDto
{
    public Guid Id { get; set; }
    public List<CandidationDistributionResultValueDto> Distribution { get; set; }
}

public class CandidationDistributionResultValueDto
{
    public double Value { get; set; }
    public int Count { get; set; }
}