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

public abstract class CandidateVoteDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; }
}

public class CandidateChoiceVoteDto : CandidateVoteDto { }

public class CandidateScoreVoteDto : CandidateVoteDto
{
    public double Score { get; set; }
}

public class CandidateRankVoteDto : CandidateVoteDto
{
    public int Rank { get; set; }
}

public class CandidateCategoryVoteDto : CandidateVoteDto
{
    public Guid CategoryId { get; set; }
}