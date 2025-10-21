using Microsoft.EntityFrameworkCore;

public class PostService(HiveMimeContext context) : IPostService
{
    public List<ListPostDto> BrowsePosts(int userId)
    {
        List<Post> posts = GetSuggestedPosts(userId);
        return posts.Select(p => p.ToListPostDto()).ToList();
    }

    public void CreatePost(int userId, CreatePostDto postDto)
    {
        IEnumerable<string> validationErrors = ValidateCreatePost(postDto);

        if (validationErrors.Any())
            throw new InvalidOperationException("Post validation failed: " + string.Join("; ", validationErrors));

        Post newPost = postDto.ToPost();
        newPost.CreatorId = userId;

        context.Posts.Add(newPost);
        context.SaveChanges();
    }

    public PollResultsDto GetPollDetails(int pollId)
    {
        Poll poll = context.Polls
            .Include(p => p.Candidates.OrderBy(c => c.Id))
                .ThenInclude(o => o.Votes)
            .First(p => p.Id == pollId);

        return poll.ToPollResultsDto();
    }

    public void UpsertVoteToPost(int userId, UpsertVoteToPostDto vote)
    {
        Post post = context.Posts
            .Include(p => p.Polls.OrderBy(p => p.Id))
                .ThenInclude(o => o.Candidates.OrderBy(c => c.Id))
            .Include(p => p.Polls.OrderBy(p => p.Id))
                .ThenInclude(o => o.Categories.OrderBy(c => c.Id))
            .First(p => p.Id == vote.PostId);

        IEnumerable<string> validationErrors = ValidatePostVotes(post, vote);

        if (validationErrors.Any())
            throw new InvalidOperationException("Vote validation failed: " + string.Join("; ", validationErrors));

        Dictionary<int, Vote> existingVotes = context.Votes
            .Where(v => v.UserId == userId && v.PostId == vote.PostId)
            .ToDictionary(v => v.CandidateId, v => v);

        foreach ((Poll poll, UpsertVoteToPollDto pollVote) in post.Polls.Zip(vote.Polls))
        {
            foreach ((Candidate candidate, UpsertVoteToCandidateDto candidateVote) in poll.Candidates.Zip(pollVote.Candidates))
            {
                // Either update the vote if one already exists, or create a new one.
                Vote dbVote = existingVotes.GetValueOrDefault(candidate.Id);

                // The user did not vote for the candidate.
                if (candidateVote.Value is null)
                {
                    if (dbVote is not null)
                        context.Votes.Remove(dbVote);

                    continue;
                }

                // The user voted for the candidate.
                if (dbVote is null)
                {
                    dbVote = new Vote
                    {
                        UserId = userId,
                        PostId = post.Id,
                        CandidateId = candidate.Id,
                        PollId = poll.Id
                    };

                    context.Votes.Add(dbVote);
                }

                dbVote.Value = candidateVote.Value.Value;
            }
        }

        context.SaveChanges();
    }

    private List<Post> GetSuggestedPosts(int userId)
    {
        List<Post> posts = context.Posts.Include(p => p.Polls.OrderBy(p => p.Id))
                                            .ThenInclude(o => o.Candidates.OrderBy(c => c.Id))
                                        .Include(p => p.Polls.OrderBy(p => p.Id))
                                            .ThenInclude(o => o.Categories.OrderBy(c => c.Id))
                                        .OrderByDescending(p => p.CreatedAt)
                                        .Take(20)
                                        .ToList();

        return posts;
    }

    private IEnumerable<string> ValidateCreatePost(CreatePostDto postDto)
    {
        if (postDto.Polls is null || !postDto.Polls.Any())
        {
            yield return "A post must contain at least one poll.";
            yield break;
        }

        foreach (CreatePollDto pollDto in postDto.Polls)
        {
            foreach (string error in ValidateCreatePoll(pollDto))
                yield return error;
        }
    }

    private IEnumerable<string> ValidateCreatePoll(CreatePollDto dto)
    {
        if (dto.PollType == PollType.Scoring)
        {
            if (dto.MinValue is null || dto.MaxValue is null || dto.StepValue is null)
                throw new InvalidOperationException("MinValue, MaxValue and StepValue must be set for scoring polls.");

            if (dto.StepValue <= 0)
                yield return "StepValue must be greater than 0.";

            if (dto.MinValue >= dto.MaxValue)
                yield return "MinValue must be less than MaxValue.";
        }
        else
        {
            dto.MinValue = 1;
            dto.MaxValue = dto.PollType switch
            {
                PollType.Ranking => Math.Min(dto.Candidates.Count, dto.MaxVotes!.Value),
                PollType.Categorization => dto.Categories.Count,
                _ => 1
            };
        }

        dto.MinVotes = Math.Clamp(dto.MinVotes ?? 1, 1, dto.Candidates.Count);

        if (dto.MaxVotes is null)
            dto.MaxVotes = dto.PollType == PollType.SingleChoice ? 1 : dto.Candidates.Count;

        dto.MaxVotes = Math.Clamp(dto.MaxVotes.Value, 1, dto.Candidates.Count);

        if (string.IsNullOrWhiteSpace(dto.Title))
            yield return "Poll title is required.";

        if (dto.Candidates is null || !dto.Candidates.Any())
            yield return "A poll must contain at least one candidate.";

        if (dto.PollType == PollType.Categorization)
        {
            if (dto.Categories is null || !dto.Categories.Any())
                yield return "A categorization poll must contain at least one category.";
        }
    }

    private IEnumerable<string> ValidatePostVotes(Post post, UpsertVoteToPostDto postVote)
    {
        if (post.Polls.Count != postVote.Polls.Count)
        {
            yield return "The number of polls voted on does not match the number of polls in the post.";
            yield break;
        }

        foreach ((Poll poll, UpsertVoteToPollDto pollVote) in post.Polls.Zip(postVote.Polls))
        {
            if (!poll.IsOptional && pollVote.Candidates.All(v => !v.Value.HasValue))
            {
                yield return $"Poll is required.";
                continue;
            }

            if (pollVote is null)
                continue;

            foreach (string error in ValidateVote(poll, pollVote!))
                yield return error;
        }
    }

    private IEnumerable<string> ValidateVote(Poll poll, UpsertVoteToPollDto pollVote)
    {
        int votesCount = pollVote.Candidates.Count(v => v.Value.HasValue);

        // General validation.
        if (pollVote.Candidates.Count != poll.Candidates.Count)
            yield return $"Poll has an invalid number of candidate votes.";

        if (votesCount < poll.MinVotes)
            yield return $"Poll requires at least {poll.MinVotes} votes.";

        if (votesCount > poll.MaxVotes)
            yield return $"Poll allows a maximum of {poll.MaxVotes} votes.";

        if (pollVote.Candidates.Any(v => v.Value.HasValue && v.Value < poll.MinValue))
            yield return $"Poll has a minimum value of {poll.MinValue}.";

        if (pollVote.Candidates.Any(v => v.Value.HasValue && v.Value > poll.MaxValue))
            yield return $"Poll has a maximum value of {poll.MaxValue}.";

        // Poll type specific validation.
        switch (poll.PollType)
        {
            case PollType.Ranking:
                foreach (string error in ValidateRankingPoll(poll, pollVote))
                    yield return error;
                break;
            default:
                break;
        }
    }

    private IEnumerable<string> ValidateRankingPoll(Poll poll, UpsertVoteToPollDto pollVote)
    {
        List<int> assignedRanks = pollVote.Candidates
            .Where(v => v.Value.HasValue)
            .Select(v => v.Value!.Value)
            .ToList();

        int expectedRankCount = assignedRanks.Count;
        HashSet<int> uniqueRanks = new(assignedRanks);

        if (uniqueRanks.Count != expectedRankCount)
            yield return "Duplicate values are not allowed in ranking polls.";

        for (int rank = 1; rank <= expectedRankCount; rank++)
        {
            if (!uniqueRanks.Contains(rank))
                yield return $"Ranking poll is missing rank {rank}.";
        }
    }
}