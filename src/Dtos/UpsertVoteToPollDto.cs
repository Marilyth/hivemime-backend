public class VoteOnPostDto
{
    public int PostId { get; set; }
    public List<VoteOnPollDto> Polls { get; set; }
}

public class VoteOnPollDto
{
    public List<VoteOnCandidateDto> Candidates { get; set; }
}

public class VoteOnCandidateDto
{
    public int? Value { get; set; }
}