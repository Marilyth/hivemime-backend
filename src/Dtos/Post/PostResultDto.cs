public class PollResultDto<T> : PollDto
{
    public new List<T> Candidates { get; set; }
}

public class CandidateResultDto
{
    public Guid Id { get; set; }
    public int VoteCount { get; set; }
}

public class CandidateSumResultDto : CandidateResultDto
{
    public int Sum { get; set; }
}

public class CandidateStatisticsResultDto : CandidateResultDto
{
    public double Min { get; set; }
    public double Q1 { get; set; }
    public double Median { get; set; }
    public double Q3 { get; set; }
    public double Max { get; set; }
    public double Average { get; set; }
}

public class CandidateDistributionResultDto : CandidateResultDto
{
    public List<CandidationDistributionResultValueDto> Distribution { get; set; }
}

public class CandidationDistributionResultValueDto
{
    public double Value { get; set; }
    public int VoteCount { get; set; }
}