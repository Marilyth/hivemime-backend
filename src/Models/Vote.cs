using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[PrimaryKey(nameof(UserId), nameof(CandidateId))]
public class Vote : Entity
{
    [ForeignKey(nameof(User))]
    public int UserId { get; set; }
    public User? User { get; set; }

    [ForeignKey(nameof(Candidate))]
    public int CandidateId { get; set; }
    public Candidate? Candidate { get; set; }

    [ForeignKey(nameof(Poll))]
    public int PollId { get; set; }
    public Poll? Poll { get; set; }

    [ForeignKey(nameof(Post))]
    public int PostId { get; set; }
    public Post? Post { get; set; }

    public int Value { get; set; }
}
