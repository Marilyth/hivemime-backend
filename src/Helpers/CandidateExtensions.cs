public static class CandidateExtensions
{
    public static Candidate ToCandidate(this PollCandidateDto option)
    {
        return new Candidate
        {
            Name = option.Name,
            Description = string.Empty
        };
    }

    public static Candidate ToCandidate(this string option)
    {
        return new Candidate
        {
            Name = option,
            Description = string.Empty
        };
    }

    public static PollCandidateDto ToCandidateDto(this Candidate option)
    {
        return new PollCandidateDto
        {
            Id = option.Id,
            Name = option.Name,
            Description = option.Description ?? string.Empty,
        };
    }

    public static PollCandidateResultDto ToCandidateResultDto(this Candidate option)
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