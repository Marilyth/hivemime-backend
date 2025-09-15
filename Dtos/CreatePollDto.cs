public abstract class CreateQuestionDto
{
    public string Title { get; set; }
    public string Description { get; set; }
    public bool AllowCustomAnswer { get; set; }
    public List<string> Options { get; set; }
}

public class CreatePollDto : CreateQuestionDto
{
    public PollAnswerType AnswerType { get; set; }
    public List<CreatePollDemographicsDto> Demographics { get; set; }
}

public class CreatePollDemographicsDto : CreateQuestionDto
{
    public DemographicsAnswerType AnswerType { get; set; }
}