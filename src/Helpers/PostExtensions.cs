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
            AnswerType = dto.PollType,
            Candidates = dto.Candidates.Select(option => new Candidate { Name = option.Name, Description = option.Description }).ToList()
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
            Candidates = poll.Candidates.Select(option => option.ToCandidateDto()).ToList()
        };
    }

    public static PollResultsDto ToPollResultsDto(this Poll poll)
    {
        return new PollResultsDto
        {
            PollType = poll.AnswerType,
            Candidates = poll.Candidates.Select(option => option.ToCandidateResultDto()).ToList()
        };
    }
}