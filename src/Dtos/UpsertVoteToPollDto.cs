public class UpsertVoteToPostDto
{
    public int PostId { get; set; }
    public List<UpsertVoteToPollDto> Polls { get; set; }
}

public class UpsertVoteToPollDto
{
    public List<UpsertVoteToCandidateDto> Candidates { get; set; }
}

public class UpsertVoteToCandidateDto
{
    public int? Value { get; set; }
}