public class PostResultsDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public List<PollResultsDto> Polls { get; set; }
}

public class PollResultsDto
{
    public string Title { get; set; }
    public string Description { get; set; }
    public PollType PollType { get; set; }
    public List<PollCandidateResultDto> Candidates { get; set; }
}

public class PollCandidateResultDto : PollCandidateDto
{
    public int Id { get; set; }
    public int VoterAmount { get; set; }
    public int Score { get; set; }
}