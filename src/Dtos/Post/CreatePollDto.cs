public class CreatePollDto
{
    public string Title { get; set; }
    public string? Description { get; set; }
    public UploadMediaRequestDto? Media { get; set; }
    public bool IsShuffled { get; set; }

    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public int AllowedCustomCandidateCount { get; set; }

    // For polls where multiple answers are allowed.
    public int MinVotes { get; set; }
    public int MaxVotes { get; set; }
    public int MinVotesPerCandidate { get; set; }
    public int MaxVotesPerCandidate { get; set; }

    public int? Rows { get; set; }
    public int? Columns { get; set; }
    
    public double? StepValue { get; set; }

    public FilterQueryBase? DateFilterQuery { get; set; }
    public bool? IgnoreTimeZone { get; set; }
    public FilterQueryBase? ConditionQuery { get; set; }

    public PollType PollType { get; set; }
    public List<CreateCandidateDto> Candidates { get; set; }
    public List<CreateCategoryDto> Categories { get; set; }
}
