public class PostVoteDto
{
    public Guid Id { get; set; }
    public List<PollVoteDto> Polls { get; set; }
}

public class PollVoteDto
{
    public Guid Id { get; set; }
    public List<CandidateVoteDto> Candidates { get; set; }
}

public class CandidateVoteDto
{
    public Guid Id { get; set; }
    public int? Value { get; set; }
}