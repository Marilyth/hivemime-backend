public class PollResultsDto
{
    public string Title { get; set; }
    public string Description { get; set; }
    public PollAnswerType PollType { get; set; }
    public List<PollOptionResultDto> PollOption { get; set; }
}

public class PollOptionResultDto : PollOptionDto
{
    public int VoterAmount { get; set; }
    public int Score { get; set; }
}