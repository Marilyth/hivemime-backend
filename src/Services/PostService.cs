using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

public class PostService(HiveMimeContext context)
{
    /// <summary>
    /// Fetches and returns a post by its ID, including all its polls and candidates.
    /// </summary>
    /// <param name="postId">The ID of the post to fetch.</param>
    public async Task<PostDto> GetPostAsync(int postId)
    {
        Post post = await context.Posts
            .AsNoTracking()
            .IncludeForBrowse()
            .FirstOrExceptionAsync(p => p.Id == postId);

        int commentCount = await context.Comments
            .CountAsync(c => c.PostId == postId && c.ParentCommentId == null);

        int voteCount = await context.PostVotes
            .CountAsync(v => v.PostId == postId);

        return post.ToPostDto(commentCount, voteCount);
    }

    /// <summary>
    /// Fetches and returns a pre selection of hot posts to show in the browse section.
    /// </summary>
    /// <param name="userId">The ID of the user browsing posts, for individual feeds.</param>
    /// <param name="afterId">The ID of the last post seen, for pagination.</param>
    /// <param name="filter">The filter to apply to the posts.</param>
    public async Task<List<PostDto>> BrowsePostsAsync(int userId, int? hiveId, int? afterId, string filter)
    {
        // TODO 5: Add reverse index for filtering posts / polls. This does not scale well.
        IQueryable<Post> posts = context.Posts.AsNoTracking();

        if (afterId.HasValue)
            posts = posts.Where(p => p.Id < afterId);

        if (hiveId.HasValue)
            posts = posts.Where(p => p.HiveId == hiveId.Value);

        if (!string.IsNullOrWhiteSpace(filter))
        {
            filter = filter.Trim().ToLower();

            posts = posts.Where(p => p.Title.ToLower().Contains(filter)
                                    || p.Description.ToLower().Contains(filter)
                                    || p.Polls.Any(poll => poll.Title.ToLower().Contains(filter)
                                        || poll.Description.ToLower().Contains(filter)));
        }

        return (await posts.IncludeForBrowse()
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(20)
                    .Select(p => new {p, CommentCount = p.Comments.Count(c => c.ParentCommentId == null), VoteCount = p.PostVotes.Count})
                    .ToListAsync())
                    .Select(a => a.p.ToPostDto(a.CommentCount, a.VoteCount)).ToList();
    }

    /// <summary>
    /// Creates and returns a new post based on the provided data.
    /// </summary>
    /// <param name="userId">The ID of the user creating the post.</param>
    /// <param name="postDto">The post to create.</param>
    public async Task<PostDto> CreatePostAsync(int userId, CreatePostDto postDto)
    {
        IEnumerable<string> validationErrors = ValidateCreatePost(postDto);
        Hive hive = null;

        if (postDto.HiveId.HasValue)
            hive = await context.Hives.FirstOrExceptionAsync(h => h.Id == postDto.HiveId.Value);

        if (validationErrors.Any())
            throw new InvalidOperationException("Post validation failed: " + string.Join("; ", validationErrors));

        Post newPost = postDto.ToPost();
        newPost.Creator = await context.Users.FindAsync(userId);
        newPost.Hive = hive;

        context.Posts.Add(newPost);
        await context.SaveChangesAsync();

        return newPost.ToPostDto(0, 0);
    }

    /// <summary>
    /// Fetches and returns the results of a post, including all its polls.
    /// </summary>
    /// <param name="postId">The ID of the post to fetch details for.</param>
    /// <param name="filter">The filter to apply to the post details.</param>
    public async Task<PostResultDto> GetPostResultAsync(int postId, string filter)
    {
        VoteQueryBase voteQuery = filter.ToVoteQuery();
        Expression<Func<PostVote, bool>> voteExpression = voteQuery.ToExpression();

        // Fetch post and filtered votes seperately for better performance.
        List<PostVote> filteredVotes = await context.PostVotes
            .AsNoTracking()
            .Where(v => v.PostId == postId)
            .Where(voteExpression)
            .Include(v => v.User.Settings)
            .Include(v => v.Votes)
            .AsSplitQuery()
            .ToListAsync();

        Post post = await context.Posts
            .AsNoTracking()
            .Include(p => p.Polls.OrderBy(p => p.Id))
                .ThenInclude(o => o.Candidates.OrderBy(c => c.Id))
            .FirstAsync(p => p.Id == postId);

        Dictionary<int, Candidate> candidateLookup = post.Polls
            .SelectMany(p => p.Candidates)
            .ToDictionary(c => c.Id);

        // Add the filtered votes to the candidates.
        candidateLookup.Values.ToList().ForEach(c => c.Votes = []);
        foreach (CandidateVote vote in filteredVotes.SelectMany(v => v.Votes))
        {
            Candidate candidate = candidateLookup[vote.CandidateId];
            candidate.Votes.Add(vote);
        }

        post.PostVotes = filteredVotes;

        return post.ToPostResultsDto();
    }

    /// <summary>
    /// Inserts or updates a user's votes on a post.
    /// </summary>
    /// <param name="userId">The ID of the user voting.</param>
    /// <param name="vote">The vote to insert or update.</param>
    public async Task VoteOnPostAsync(int userId, VoteOnPostDto vote)
    {
        Post post = await context.Posts
            .Include(p => p.Polls.OrderBy(p => p.Id))
                .ThenInclude(o => o.Candidates.OrderBy(c => c.Id))
            .Include(p => p.Polls.OrderBy(p => p.Id))
                .ThenInclude(o => o.Categories.OrderBy(c => c.Id))
            .FirstAsync(p => p.Id == vote.PostId);

        IEnumerable<string> validationErrors = ValidatePostVotes(post, vote);

        if (validationErrors.Any())
            throw new InvalidOperationException("Vote validation failed: " + string.Join("; ", validationErrors));
            
        PostVote postVote = await context.PostVotes
            .Include(pv => pv.Votes)
            .FirstOrDefaultAsync(pv => pv.UserId == userId && pv.PostId == vote.PostId);

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

        await context.SaveChangesAsync();
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