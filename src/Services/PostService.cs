using Microsoft.EntityFrameworkCore;

public class PostService(HiveMimeContext context) : IPostService
{
    public List<PostDto> BrowsePosts(int userId, int? afterId, string filter)
    {
        List<Post> posts = GetSuggestedPosts(userId, afterId, filter);
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

    public PostResultDto GetPostDetails(int postId, string filter)
    {
        // TODO 9: Apply filter to post details.
        Post post = context.Posts
            .AsNoTracking()
            .Include(p => p.PostVotes)
                .ThenInclude(v => v.User)
                    .ThenInclude(u => u.Settings)
            .Include(p => p.Polls.OrderBy(p => p.Id))
                .ThenInclude(o => o.Candidates.OrderBy(c => c.Id))
                    .ThenInclude(o => o.Votes)
            .AsSplitQuery()
            .First(p => p.Id == postId);

        return post.ToPostResultsDto();
    }

    public void VoteOnPost(int userId, VoteOnPostDto vote, string country)
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
            
        PostVote postVote = context.PostVotes
            .Include(pv => pv.Votes)
            .FirstOrDefault(pv => pv.UserId == userId && pv.PostId == vote.PostId);

        if (postVote is null)
        {
            postVote = new PostVote
            {
                UserId = userId,
                PostId = post.Id,
                Votes = new List<CandidateVote>()
            };

            context.PostVotes.Add(postVote);
        }

        foreach ((Poll poll, VoteOnPollDto pollVote) in post.Polls.Zip(vote.Polls))
        {
            foreach ((Candidate candidate, VoteOnCandidateDto candidateVote) in poll.Candidates.Zip(pollVote.Candidates))
            {
                // Either update the vote if one already exists, or create a new one.
                CandidateVote dbVote = postVote.Votes.FirstOrDefault(v => v.CandidateId == candidate.Id);

                // The user did not vote for the candidate.
                if (candidateVote.Value is null)
                {
                    if (dbVote is not null)
                        context.CandidateVotes.Remove(dbVote);

                    continue;
                }

                // The user voted for the candidate.
                if (dbVote is null)
                {
                    dbVote = new CandidateVote
                    {
                        CandidateId = candidate.Id,
                        PostVote = postVote
                    };

                    postVote.Votes.Add(dbVote);
                }

                dbVote.Value = candidateVote.Value.Value;
            }
        }

        context.SaveChanges();
    }

    private List<Post> GetSuggestedPosts(int userId, int? afterId, string? filter)
    {
        // TODO 5: Add reverse index for filtering posts / polls. This does not scale well.
        IQueryable<Post> posts = context.Posts.AsNoTracking();

        if (afterId.HasValue)
        {
            posts = posts.Where(p => p.Id < afterId);
        }

        if (!string.IsNullOrWhiteSpace(filter))
        {
            filter = filter.Trim().ToLower();

            posts = posts.Where(p => p.Title.ToLower().Contains(filter)
                                    || p.Description.ToLower().Contains(filter)
                                    || p.Polls.Any(poll => poll.Title.ToLower().Contains(filter)
                                        || poll.Description.ToLower().Contains(filter)));
        }

        return posts.Include(p => p.Polls.OrderBy(p => p.Id))
                        .ThenInclude(o => o.Candidates.OrderBy(c => c.Id))
                    .Include(p => p.Polls.OrderBy(p => p.Id))
                        .ThenInclude(o => o.Categories.OrderBy(c => c.Id))
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(20).ToList();
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
        dto.MinVotes = Math.Clamp(dto.MinVotes, 1, dto.Candidates.Count);

        if (dto.MaxVotes == -1)
            dto.MaxVotes = dto.Candidates.Count;
        else
            dto.MaxVotes = Math.Clamp(dto.MaxVotes, dto.MinVotes, dto.Candidates.Count);

        if (dto.PollType == PollType.Score)
        {
            if (dto.StepValue is null)
                throw new InvalidOperationException("StepValue must be set for scoring polls.");

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
                PollType.Rank => dto.MaxVotes,
                PollType.Category => dto.Categories.Count,
                _ => 1
            };
        }

        if (string.IsNullOrWhiteSpace(dto.Title))
            yield return "Poll title is required.";

        else if (dto.Title.Trim().Length < 3)
            yield return "Poll title must be at least 3 characters long.";

        if (dto.Candidates is null || !dto.Candidates.Any())
            yield return "A poll must contain at least one candidate.";

        if (dto.PollType == PollType.Category)
        {
            if (dto.Categories is null || !dto.Categories.Any())
                yield return "A categorization poll must contain at least one category.";
        }
    }

    private IEnumerable<string> ValidatePostVotes(Post post, VoteOnPostDto postVote)
    {
        if (post.Polls.Count != postVote.Polls.Count)
        {
            yield return "The number of polls voted on does not match the number of polls in the post.";
            yield break;
        }

        foreach ((Poll poll, VoteOnPollDto pollVote) in post.Polls.Zip(postVote.Polls))
        {
            if (!poll.IsOptional && pollVote.Candidates.All(v => !v.Value.HasValue))
            {
                yield return $"Voting on the poll is required.";
                continue;
            }

            foreach (string error in ValidateVote(poll, pollVote!))
                yield return error;
        }
    }

    private IEnumerable<string> ValidateVote(Poll poll, VoteOnPollDto pollVote)
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
            case PollType.Rank:
                foreach (string error in ValidateRankingPoll(poll, pollVote))
                    yield return error;
                break;
            default:
                break;
        }
    }

    private IEnumerable<string> ValidateRankingPoll(Poll poll, VoteOnPollDto pollVote)
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