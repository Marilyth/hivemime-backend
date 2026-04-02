public class CreatePostDto
{
    public int? HiveId { get; set; }
    
    public string? Title { get; set; }
    public string? Description { get; set; }
    public List<CreatePollDto> Polls { get; set; }
}

public class CreatePollDto
{
    public string Title { get; set; }
    public string? Description { get; set; }
    public bool IsShuffled { get; set; }
    public bool IsOptional { get; set; }

    public int MinValue { get; set; }
    public int MaxValue { get; set; }
    public double? StepValue { get; set; }

    // For polls where multiple answers are allowed.
    public int MinVotes { get; set; }
    public int MaxVotes { get; set; }

    public PollType PollType { get; set; }
    public List<CreateCandidateDto> Candidates { get; set; }
    public List<CreateCategoryDto> Categories { get; set; }
}

public class CreateCandidateDto
{
    public string Name { get; set; }
    public string? Description { get; set; }
}

public class CreateCategoryDto
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public int Color { get; set; }
}