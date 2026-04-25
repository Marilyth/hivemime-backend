public class PostVoteDto
{
    public int PostId { get; set; }
    public List<PollVoteDto> Polls { get; set; }
}

public class PollVoteDto
{
    public List<CandidateVoteDto> Candidates { get; set; }
}

public class CandidateVoteDto
{
    public int? Value { get; set; }
}