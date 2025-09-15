public static class PollExtensions
{
    public static Poll ToPoll(this CreatePollDto dto)
    {
        return new Poll
        {
            Title = dto.Title,
            Description = dto.Description,
            AnswerType = dto.AnswerType,
            Options = dto.Options?.Select(option => new AnswerOption { Text = option }).ToList() ?? new List<AnswerOption>(),
            Demographics = dto.Demographics?.Select(demo => demo.ToDemographics()).ToList() ?? new List<Demographics>()
        };
    }

    public static GetPollDto ToPollOverviewDto(this Poll poll)
    {
        return new GetPollDto
        {
            Id = poll.Id,
            Title = poll.Title,
            Description = poll.Description,
            PollType = poll.AnswerType,
            Options = poll.Options?.Select(option => option.ToAnswerOptionDto()).ToList() ?? new List<AnswerOptionDto>()
        };
    }
}