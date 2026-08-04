public class PollDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public List<string> MediaKeys { get; set; }
    public string? Description { get; set; }
    public int AllowedCustomCandidateCount { get; set; }
    public bool IsShuffled { get; set; }

    public int MinValue { get; set; }
    public int MaxValue { get; set; }

    public int MinVotes { get; set; }
    public int MaxVotes { get; set; }
    public int MinVotesPerCandidate { get; set; }
    public int MaxVotesPerCandidate { get; set; }

    public int? Rows { get; set; }
    public int? Columns { get; set; }

    public double? StepValue { get; set; }
    public string? DateFilterQuery { get; set; }
    public string? ConditionQuery { get; set; }

    public PollType PollType { get; set; }
    public List<CandidateDto> Candidates { get; set; }
    public List<CategoryDto> Categories { get; set; }
}
