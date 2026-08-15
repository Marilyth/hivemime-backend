using System.Text.Json.Serialization;

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

[JsonConverter(typeof(CandidateVoteDtoConverter))]
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

public class CandidateGridVoteDto : CandidateVoteDto
{
    public int Row { get; set; }
    public int Column { get; set; }
}

public class CandidateDateVoteDto : CandidateVoteDto
{
    public long Timestamp { get; set; }
}