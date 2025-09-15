public abstract class Question : ModelBase
{
    public string Title { get; set; }
    public string Description { get; set; }
    public bool AllowCustomAnswer { get; set; }
    public List<AnswerOption> Options { get; set; }
}

public class Demographics : Question
{
    public DemographicsAnswerType AnswerType { get; set; }
}

public enum DemographicsAnswerType
{
    SingleChoice,
    MultipleChoice
}

public class Poll : Question
{
    public PollAnswerType AnswerType { get; set; }
    public List<Demographics> Demographics { get; set; }
}

public enum PollAnswerType
{
    SingleChoice,
    MultipleChoice,
    Ranking,
    Categorization
}