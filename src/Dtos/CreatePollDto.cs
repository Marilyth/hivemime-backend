public class CreatePostDto
{
    public string Title { get; set; }
    public string? Description { get; set; }
    public List<CreatePollDto> Polls { get; set; }
}

public class CreatePollDto
{
    public string Title { get; set; }
    public string? Description { get; set; }
    public bool AllowCustomAnswer { get; set; }

    public int MinValue { get; set; }
    public int MaxValue { get; set; }
    public double StepValue { get; set; }

    // For polls where multiple answers are allowed.
    public int MinVotes { get; set; }
    public int MaxVotes { get; set; }

    public PollType PollType { get; set; }
    public List<PollCandidateDto> Candidates { get; set; }
    public List<PollCategoryDto> Categories { get; set; }
}