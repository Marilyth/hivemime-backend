public class CreatePostDto
{
    public string Title { get; set; }
    public string Description { get; set; }
    public List<CreatePollDto> Polls { get; set; }
}

public class CreatePollDto
{
    public string Title { get; set; }
    public string Description { get; set; }
    public bool AllowCustomAnswer { get; set; }
    public PollAnswerType AnswerType { get; set; }
    public List<PollOptionDto> Options { get; set; }
}