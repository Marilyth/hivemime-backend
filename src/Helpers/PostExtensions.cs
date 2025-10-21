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
        return new()
        {
            Title = dto.Title,
            Description = dto.Description,
            AllowCustomAnswer = dto.AllowCustomAnswer,
            IsShuffled = dto.IsShuffled,
            IsOptional = dto.IsOptional,
            PollType = dto.PollType,
            MinValue = dto.MinValue!.Value,
            MaxValue = dto.MaxValue!.Value,
            StepValue = dto.StepValue,
            MinVotes = dto.MinVotes!.Value,
            MaxVotes = dto.MaxVotes!.Value,
            Categories = dto.Categories.Select(category => category.ToCategory()).ToList(),
            Candidates = dto.Candidates.Select(candidate => candidate.ToCandidate()).ToList()
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
            Title = poll.Title,
            Description = poll.Description,
            PollType = poll.PollType,
            AllowCustomAnswer = poll.AllowCustomAnswer,
            IsShuffled = poll.IsShuffled,
            IsOptional = poll.IsOptional,
            MinValue = poll.MinValue,
            MaxValue = poll.MaxValue,
            StepValue = poll.StepValue,
            MinVotes = poll.MinVotes,
            MaxVotes = poll.MaxVotes,
            Categories = poll.Categories.Select(category => category.ToCategoryDto()).ToList(),
            Candidates = poll.Candidates.Select(option => option.ToCandidateDto()).ToList()
        };
    }

    public static PollResultsDto ToPollResultsDto(this Poll poll)
    {
        return new PollResultsDto
        {
            PollType = poll.PollType,
            Candidates = poll.Candidates.Select(option => option.ToCandidateResultDto()).ToList()
        };
    }
}