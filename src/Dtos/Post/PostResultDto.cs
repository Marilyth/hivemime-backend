public class PollResultDto<T> : PollDto where T : CandidateResultDto
{
    public new List<T> Candidates { get; set; }
}

public class CandidateResultDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public bool IsCustom { get; set; }
    public int VoteCount { get; set; }
}

public class CandidateChoiceResultDto : CandidateResultDto { }

public class CandidateScoreResultDto : CandidateResultDto
{
    public double Min { get; set; }
    public double Q1 { get; set; }
    public double Median { get; set; }
    public double Q3 { get; set; }
    public double Max { get; set; }
    public double Average { get; set; }
}

public class CandidateRankResultDto : CandidateResultDto
{
    public List<CandidateRankDistributionResultDto> Distribution { get; set; }
}

public class CandidateRankDistributionResultDto
{
    public int Rank { get; set; }
    public int VoteCount { get; set; }
}

public class CandidateCategoryResultDto : CandidateResultDto
{
    public List<CandidateCategoryDistributionResultDto> Distribution { get; set; }
}

public class CandidateCategoryDistributionResultDto
{
    public Guid CategoryId { get; set; }
    public int VoteCount { get; set; }
}

public class CandidateDrawResultDto : CandidateResultDto
{
    public List<CandidateDrawDistributionResultDto> Distribution { get; set; }
}

public class CandidateDrawDistributionResultDto
{
    public int CellIndex { get; set; }
    public double Value { get; set; }
    public int VoteCount { get; set; }
}