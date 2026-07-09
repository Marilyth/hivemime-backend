using System.ComponentModel.DataAnnotations.Schema;

public class PostVote : EntityWithIdentifier
{
    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }
    public User? User { get; set; }

    [ForeignKey(nameof(Post))]
    public Guid PostId { get; set; }
    public Post? Post { get; set; }

    public List<CandidateVote> Votes { get; set; }
}

public abstract class CandidateVote : EntityWithIdentifier
{
    [ForeignKey(nameof(Candidate))]
    public Guid CandidateId { get; set; }
    public Candidate? Candidate { get; set; }

    [ForeignKey(nameof(PostVote))]
    public Guid PostVoteId { get; set; }
    public PostVote? PostVote { get; set; }
}

public class CandidateChoiceVote : CandidateVote { }

public class CandidateScoreVote : CandidateVote
{
    public double Score { get; set; }
}

public class CandidateRankVote : CandidateVote
{
    public int Rank { get; set; }
}

public class CandidateCategoryVote : CandidateVote
{
    [ForeignKey(nameof(Category))]
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
}

public class CandidateDrawVote : CandidateVote
{
    public double Value { get; set; }
    public int CellIndex { get; set; }
}
