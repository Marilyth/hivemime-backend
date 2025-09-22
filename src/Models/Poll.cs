using System.ComponentModel.DataAnnotations.Schema;

public class Poll : EntityWithIdentifier
{
    public string Title { get; set; }
    public string Description { get; set; }
    public bool AllowCustomAnswer { get; set; }

    public PollAnswerType AnswerType { get; set; }
    public List<PollOption> Options { get; set; }

    [ForeignKey(nameof(Post))]
    public int PostId { get; set; }
    public Post? Post { get; set; }
}

public enum PollAnswerType
{
    SingleChoice,
    MultipleChoice,
    Ranking,
    Categorization
}