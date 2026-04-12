public class PostResultDto
{
    public List<PollResultDto> Polls { get; set; }
}

public class PollResultDto
{
    public List<PollCandidateResultDto> Candidates { get; set; }
}

public class PollCandidateResultDto : CandidateDto
{
    public int VoterAmount { get; set; }
    public double? AverageScore { get; set; }
    public int? MajorityVote { get; set; }
    public double? MajorityRatio { get; set; }
}

public class CandidateResultDto
{
    public int Id { get; set; }
    public List<CandidateDistributionDto> Distribution { get; set; }
}

public class CandidateDistributionDto
{
    public int Score { get; set; }
}