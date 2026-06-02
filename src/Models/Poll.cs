using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Poll : EntityWithIdentifier
{
    [MaxLength(128)]
    public string Title { get; set; }
    [MaxLength(1024)]
    public string? Description { get; set; }
    public List<string> MediaKeys { get; set; } = [];

    public bool AllowCustomAnswer { get; set; }
    public bool IsShuffled { get; set; }

    public int MinValue { get; set; }
    public int MaxValue { get; set; }
    public double? StepValue { get; set; }

    /// <summary>
    /// Gets or sets the minimum number of votes a user must cast in this poll.
    /// 0 means this poll is optional.
    /// </summary>
    public int MinVotes { get; set; }
    public int MaxVotes { get; set; }

    public PollType PollType { get; set; }
    public List<Candidate> Candidates { get; set; } = [];
    public List<Category> Categories { get; set; } = [];

    [ForeignKey(nameof(Post))]
    public Guid PostId { get; set; }
    public Post? Post { get; set; }
    
    public int Order { get; set; }
}

public enum PollType
{
    Choice,
    Score,
    Rank,
    Category
}