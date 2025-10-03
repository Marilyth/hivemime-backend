using System.ComponentModel.DataAnnotations.Schema;

public class Poll : EntityWithIdentifier
{
    public string Title { get; set; }
    public string? Description { get; set; }
    public bool AllowCustomAnswer { get; set; }
    public bool IsShuffled { get; set; }

    public int MinValue { get; set; }
    public int MaxValue { get; set; }
    public double StepValue { get; set; }

    /// <summary>
    /// Gets or sets the minimum number of votes a user must cast in this poll.
    /// 0 means this poll is optional.
    /// </summary>
    public int MinVotes { get; set; }
    public int MaxVotes { get; set; }

    public PollType PollType { get; set; }
    public List<Candidate> Candidates { get; set; }
    public List<Category> Categories { get; set; }

    [ForeignKey(nameof(Post))]
    public int PostId { get; set; }
    public Post? Post { get; set; }
}

public enum PollType
{
    SingleChoice,
    MultipleChoice,
    Scoring,
    Ranking,
    Categorization
}