public class UpsertVoteToPostDto
{
    public int PostId { get; set; }
    public List<UpsertVoteToPollDto> Polls { get; set; }
}

public class UpsertVoteToPollDto
{
    public int PollId { get; set; }
    public List<UpsertVoteToCandidateDto> Candidates { get; set; }
}

public class UpsertVoteToCandidateDto
{
    public int CandidateId { get; set; }
    public int? Value { get; set; }
}