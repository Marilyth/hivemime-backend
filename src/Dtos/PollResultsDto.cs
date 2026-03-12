public class PostResultDto
{
    public List<PollResultDto> Polls { get; set; }
}

public class PollResultDto
{
    public PollType PollType { get; set; }
    public List<PollCandidateResultDto> Candidates { get; set; }
}

public class PollCandidateResultDto : PollCandidateDto
{
    public int VoterAmount { get; set; }
    public int Score { get; set; }
}