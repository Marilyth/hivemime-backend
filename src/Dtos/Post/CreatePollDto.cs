public class CreatePollDto
{
    public string Title { get; set; }
    public string? Description { get; set; }
    public UploadMediaRequestDto? Media { get; set; }
    public bool IsShuffled { get; set; }

    public int MinValue { get; set; }
    public int MaxValue { get; set; }
    public int AllowedCustomCandidateCount { get; set; }
    public double? StepValue { get; set; }

    // For polls where multiple answers are allowed.
    public int MinVotes { get; set; }
    public int MaxVotes { get; set; }
    public int MinVotesPerCandidate { get; set; }
    public int MaxVotesPerCandidate { get; set; }

    public PollType PollType { get; set; }
    public List<CreateCandidateDto> Candidates { get; set; }
    public List<CreateCategoryDto> Categories { get; set; }
}
