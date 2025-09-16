using System.ComponentModel.DataAnnotations.Schema;

public class Poll : EntityWithIdentifier
{
    public string Title { get; set; }
    public string Description { get; set; }
    public bool AllowCustomAnswer { get; set; }
    public PollAnswerType AnswerType { get; set; }
    public List<PollOption> Options { get; set; }
    public List<Poll> SubPolls { get; set; }

    [ForeignKey(nameof(Creator))]
    public int CreatorId { get; set; }
    public User? Creator { get; set; }

    [ForeignKey(nameof(ParentPoll))]
    public int? ParentPollId { get; set; }
    public Poll? ParentPoll { get; set; }
}

public enum PollAnswerType
{
    SingleChoice,
    MultipleChoice,
    Ranking,
    Categorization
}