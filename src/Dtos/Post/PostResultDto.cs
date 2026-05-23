public class PostResultDto
{
    public List<PollResultDto> Polls { get; set; }
}

public class PollResultDto : PollDto
{
    public new List<PollCandidateResultDto> Candidates { get; set; }
}

public class PollCandidateResultDto : CandidateDto
{
    public int VoterAmount { get; set; }
    public double? AverageScore { get; set; }
    public int? MajorityVote { get; set; }
    public double? MajorityRatio { get; set; }
}

public class CandidateDistributionDto
{
    public int Value { get; set; }
    public int Score { get; set; }
}