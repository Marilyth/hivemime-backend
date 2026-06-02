using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class CandidateVote : EntityWithIdentifier
{
    [ForeignKey(nameof(Candidate))]
    public Guid CandidateId { get; set; }
    public Candidate? Candidate { get; set; }

    [ForeignKey(nameof(PostVote))]
    public Guid PostVoteId { get; set; }
    public PostVote? PostVote { get; set; }

    public int Value { get; set; }
}

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
