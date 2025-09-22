public static class PollExtensions
{
    public static Post ToPost(this CreatePostDto dto)
    {
        return new Post
        {
            Title = dto.Title,
            Description = dto.Description,
            Polls = dto.Polls.Select(pollDto => pollDto.ToPoll()).ToList()
        };
    }

    public static Poll ToPoll(this CreatePollDto dto)
    {
        return new Poll
        {
            Title = dto.Title,
            Description = dto.Description,
            AllowCustomAnswer = dto.AllowCustomAnswer,
            AnswerType = dto.AnswerType,
            Options = dto.Options.Select(option => new PollOption { Name = option.Name, Description = option.Description }).ToList()
        };
    }

    public static ListPostDto ToListPostDto(this Post post)
    {
        return new ListPostDto
        {
            Id = post.Id,
            Title = post.Title,
            Description = post.Description,
            Polls = post.Polls.Select(poll => poll.ToListPollDto()).ToList()
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