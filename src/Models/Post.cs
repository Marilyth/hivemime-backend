using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[Index(nameof(Hotness))]
public class Post : EntityWithIdentifier
{
    public List<Poll> Polls { get; set; }
    public List<Comment> Comments { get; set; }
    public List<PostVote> PostVotes { get; set; }

    public int CommentCount { get; set; }
    public int VoteCount { get; set; }
    public double Hotness { get; set; }

    [ForeignKey(nameof(Creator))]
    public int CreatorId { get; set; }
    public User Creator { get; set; }

    [ForeignKey(nameof(Hive))]
    public int? HiveId { get; set; }
    public Hive? Hive { get; set; }

    public DateTimeOffset HotnessLastRecalculatedAt { get; set; } = DateTimeOffset.UtcNow;
}