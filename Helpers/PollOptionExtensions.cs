public static class AnswerOptionExtensions
{
    public static PollOption ToPollOption(this PollOptionDto option)
    {
        return new PollOption
        {
            Name = option.Name,
            Description = string.Empty
        };
    }

    public static PollOption ToPollOption(this string option)
    {
        return new PollOption
        {
            Name = option,
            Description = string.Empty
        };
    }

    public static PollOptionDto ToPollOptionDto(this PollOption option)
    {
        return new PollOptionDto
        {
            Name = option.Name,
            Description = option.Description ?? string.Empty,
        };
    }

    public static PollOptionResultDto ToPollOptionResultDto(this PollOption option)
    {
        return new PollOptionResultDto
        {
            Name = option.Name,
            Description = option.Description ?? string.Empty,
            VoterAmount = option.Votes.Count,
            Score = option.Votes.Sum(vote => vote.Value)
        };
    }
}