public class PostResultsDto
{
    public List<PollResultsDto> Polls { get; set; }
    public PollResultsDto CountryPoll { get; set; }
    public PollResultsDto DatePoll { get; set; }
}

public class PollResultsDto
{
    public PollType PollType { get; set; }
    public List<PollCandidateResultDto> Candidates { get; set; }
}

public class PollCandidateResultDto : PollCandidateDto
{
    public int VoterAmount { get; set; }
    public int Score { get; set; }
}