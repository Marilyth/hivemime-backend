public static class PollExtensions
{
    public static Poll ToPoll(this CreatePollDto dto)
    {
        return new Poll
        {
            Title = dto.Title,
            Description = dto.Description,
            AnswerType = dto.AnswerType,
            Options = dto.Options.Select(option => new PollOption { Name = option.Name, Description = option.Description }).ToList(),
            SubPolls = dto.SubPolls.Select(demo => demo.ToPoll()).ToList()
        };
    }

    public static ListPollDto ToListPollDto(this Poll poll)
    {
        return new ListPollDto
        {
            Id = poll.Id,
            Title = poll.Title,
            Description = poll.Description,
            PollType = poll.AnswerType,
            Options = poll.Options.Select(option => option.ToPollOptionDto()).ToList()
        };
    }

    public static PollResultsDto ToPollResultsDto(this Poll poll)
    {
        return new PollResultsDto
        {
            Title = poll.Title,
            Description = poll.Description,
            PollType = poll.AnswerType,
            PollOption = poll.Options.Select(option => option.ToPollOptionResultDto()).ToList()
        };
    }
}