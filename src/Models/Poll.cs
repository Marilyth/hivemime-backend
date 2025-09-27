using System.ComponentModel.DataAnnotations.Schema;

public class Poll : EntityWithIdentifier
{
    public string Title { get; set; }
    public string Description { get; set; }
    public bool AllowCustomAnswer { get; set; }

    public PollType AnswerType { get; set; }
    public List<Candidate> Candidates { get; set; }

    [ForeignKey(nameof(Post))]
    public int PostId { get; set; }
    public Post? Post { get; set; }
}

public enum PollType
{
    SingleChoice,
    MultipleChoice,
    Rating,
    Ranking,
    Categorization
}