public class ListPostDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public List<ListPollDto> Polls { get; set; }
}

public class ListPollDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }

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