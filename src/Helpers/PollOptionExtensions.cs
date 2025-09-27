public static class AnswerOptionExtensions
{
    public static Candidate ToPollOption(this PollCandidateDto option)
    {
        return new Candidate
        {
            Name = option.Name,
            Description = string.Empty
        };
    }

    public static Candidate ToPollOption(this string option)
    {
        return new Candidate
        {
            Name = option,
            Description = string.Empty
        };
    }

    public static PollCandidateDto ToPollCandidateDto(this Candidate option)
    {
        return new PollCandidateDto
        {
            Id = option.Id,
            Name = option.Name,
            Description = option.Description ?? string.Empty,
        };
    }

    public static PollCandidateResultDto ToPollOptionResultDto(this Candidate option)
    {
        return new PollCandidateResultDto
        {
            Name = option.Name,
            Description = option.Description ?? string.Empty,
            VoterAmount = option.Votes.Count,
            Score = option.Votes.Sum(vote => vote.Value)
        };
    }
}