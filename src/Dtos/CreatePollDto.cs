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
    public PollType PollType { get; set; }
    public List<PollCandidateDto> Candidates { get; set; }
}