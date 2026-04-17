using System.Linq.Expressions;
using Mapster;
using Microsoft.EntityFrameworkCore;

public class PostService(HiveMimeContext context, HotnessUpdateQueue hotnessQueue)
{
    /// <summary>
    /// Fetches and returns a post by its ID, including all its polls and candidates.
    /// </summary>
    /// <param name="postId">The ID of the post to fetch.</param>
    public async Task<PostDto> GetPostAsync(int postId)
    {
        return await context.Posts
            .AsNoTracking()
            .QueryableFind(postId)
            .ProjectToType<PostDto>()
            .FirstAsync();
    }

    /// <summary>
    /// Fetches and returns a pre selection of hot posts to show in the browse section.
    /// </summary>
    /// <param name="creatorId">The ID of the user to fetch posts from.</param>
    /// <param name="filter">The filter to apply to the posts.</param>
    /// <param name="pagination">The pagination parameters.</param>
    public async Task<List<PostDto>> BrowsePostsAsync(int? creatorId, int? hiveId, string filter, PostPaginationDto pagination)
    {
        pagination.PageSize = Math.Clamp(pagination.PageSize, 1, 100);

        IQueryable<Post> posts = context.Posts.AsNoTracking();

        if (creatorId.HasValue)
            posts = posts.Where(p => p.CreatorId == creatorId.Value);
            
        if (hiveId.HasValue)
            posts = posts.Where(p => p.HiveId == hiveId.Value);

        posts = await posts.ApplyPaginationFilterAsync(pagination);

        // TODO 5: Add reverse index for filtering posts / polls. This does not scale well.
        if (!string.IsNullOrWhiteSpace(filter))
        {
            filter = filter.Trim().ToLower();

            posts = posts.Where(p => p.Title.ToLower().Contains(filter)
                                    || p.Description.ToLower().Contains(filter)
                                    || p.Polls.Any(poll => poll.Title.ToLower().Contains(filter)
                                        || poll.Description.ToLower().Contains(filter)));
        }

        posts = posts.ApplyPaginationOrdering(pagination).Take(pagination.PageSize);
        var postsToUpdate = await posts.Where(p => DateTimeOffset.UtcNow - p.HotnessLastRecalculatedAt > TimeSpan.FromMinutes(60))
            .Select(p => p.Id).ToListAsync();

        hotnessQueue.AddPosts(postsToUpdate);

        return await posts.ProjectToType<PostDto>().ToListAsync();
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

        Post newPost = postDto.Adapt<Post>();

        // Each category requires a value for easier evaluation and filtering.
        foreach (Poll poll in newPost.Polls.Where(p => p.PollType == PollType.Category))
        {
            for (int i = 0; i < poll.Categories.Count; i++)
                poll.Categories[i].Value = i + 1;
        }

        newPost.Creator = await context.Users.FindAsync(userId);
        newPost.Hive = hive;

        context.Posts.Add(newPost);
        await context.SaveChangesAsync();

        return await context.Posts
            .AsNoTracking()
            .QueryableFind(newPost.Id)
            .ProjectToType<PostDto>()
            .FirstAsync();
    }

    /// <summary>
    /// Fetches and returns the results of a post, including all its polls.
    /// </summary>
    /// <param name="postId">The ID of the post to fetch details for.</param>
    /// <param name="filter">The filter to apply to the post details.</param>
    public async Task<PostResultDto> GetPostResultAsync(int postId, string filter)
    {
        // Fetch the results and DTO structure seperately for better performance.
        PostResultDto resultDto = await context.Posts.Where(p => p.Id == postId)
            .ProjectToType<PostResultDto>()
            .FirstAsync();

        VoteQueryBase voteQuery = filter.ToVoteQuery();
        Expression<Func<PostVote, bool>> voteExpression = voteQuery.ToExpression();

        IQueryable<PostVote> filteredVotes = context.PostVotes
            .Where(v => v.PostId == postId)
            .Where(voteExpression);
        
        IQueryable<CandidateVote> candidateVotes = filteredVotes.SelectMany(v => v.Votes);
        Dictionary<int, PollCandidateResultDto> candidateResults = await GetCandidateResultsAsync(candidateVotes);

        // Merge the results into the structure.
        foreach (PollCandidateResultDto candidateResult in resultDto.Polls.SelectMany(p => p.Candidates))
        {
            // Candidates unvoted for will remain default, that is okay.
            if (candidateResults.TryGetValue(candidateResult.Id, out var dbResult))
            {
                candidateResult.VoterAmount = dbResult.VoterAmount;
                candidateResult.AverageScore = dbResult.AverageScore;
                candidateResult.MajorityVote = dbResult.MajorityVote;
                candidateResult.MajorityRatio = dbResult.MajorityRatio;
            }
        }

        // TODO: Add auto polls at this point later.

        return resultDto;
    }

    /// <summary>
    /// Returns the distribution of votes for a specific candidate. I.e. the number of votes of each of its possible values.
    /// </summary>
    /// <param name="candidateId">The ID of the candidate to fetch distribution for.</param>
    /// <param name="filter">The filter to apply to the candidate votes.</param>
    public async Task<List<CandidateDistributionDto>> GetCandidateDistributionResultsAsync(int candidateId, string filter)
    {
        var candidateInfo = await context.Candidates.Where(c => c.Id == candidateId)
            .Select(c => new { c.Poll.PostId, c.Poll.MaxValue, c.Poll.MinValue, c.Poll.PollType})
            .FirstAsync();

        VoteQueryBase voteQuery = filter.ToVoteQuery();
        Expression<Func<PostVote, bool>> voteExpression = voteQuery.ToExpression();

        // Fetch post and filtered votes seperately for better performance.
        IQueryable<CandidateVote> filteredVotes = context.PostVotes
            .AsNoTracking()
            .Where(v => v.PostId == candidateInfo.PostId)
            .Where(voteExpression)
            .SelectMany(v => v.Votes.Where(cv => cv.CandidateId == candidateId));

        IQueryable<IGrouping<int, CandidateVote>> distributionQuery = null;

        if (candidateInfo.PollType == PollType.Score && candidateInfo.MaxValue - candidateInfo.MinValue > 10)
        {
            // Bucket up the votes for score polls because of the large amount of possible values.
            int stepValue = (int)Math.Ceiling((candidateInfo.MaxValue - candidateInfo.MinValue + 1) / 10.0);
            distributionQuery = filteredVotes.GroupBy(v => ((v.Value - candidateInfo.MinValue) / stepValue) * stepValue + candidateInfo.MinValue);
        }
        else
        {
            distributionQuery = filteredVotes.GroupBy(v => v.Value);        
        }

        return await distributionQuery
            .OrderBy(g => g.Key)
            .Select(g => new CandidateDistributionDto
            {
                Value = g.Key,
                Score = g.Count(),
            })
            .ToListAsync();
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

    private async Task<Dictionary<int, PollCandidateResultDto>> GetCandidateResultsAsync(IQueryable<CandidateVote> candidateVotes)
    {
        // TODO: Split this over multiple DbContexts for parallelization.
        
        // Split up the aggregation per aggregation function type.
        var countCandidates = await candidateVotes
            .Where(v => v.Candidate.Poll.PollType == PollType.Choice)
            .GroupBy(v => v.CandidateId)
            .Select(g => new PollCandidateResultDto
            {
                Id = g.Key,
                VoterAmount = g.Count()
             })
             .ToDictionaryAsync(g => g.Id);
             
        var averageCandidates = await candidateVotes
            .Where(v => v.Candidate.Poll.PollType == PollType.Rank || v.Candidate.Poll.PollType == PollType.Score)
            .GroupBy(v => v.CandidateId)
            .Select(g => new PollCandidateResultDto
            {
                Id = g.Key,
                VoterAmount = g.Count(),
                AverageScore = g.Average(v => v.Value)
             })
             .ToDictionaryAsync(g => g.Id);

        var majorityCandidates = await candidateVotes
            .Where(v => v.Candidate.Poll.PollType == PollType.Category)
            .GroupBy(v => v.CandidateId)
            .Select(g => new
            {
                Id = g.Key,
                Total = g.Count(),
                // SQL is unable to drill down a grouping to get the majority. We need to fetch the distribution.
                Distribution = g
                    .GroupBy(x => x.Value)
                    .Select(x => new { Value = x.Key, Count = x.Count() })
            })
            .ToListAsync()
            .ContinueWith(t => t.Result.ToDictionary(x => x.Id, x =>
            {
                var majorityVote = x.Distribution.OrderByDescending(d => d.Count).First();
                return new PollCandidateResultDto
                {
                    Id = x.Id,
                    VoterAmount = x.Total,
                    MajorityVote = majorityVote.Value,
                    MajorityRatio = (double)majorityVote.Count / x.Total
                };
            }));
        
        var results = new Dictionary<int, PollCandidateResultDto>();

        foreach (var task in new[] { countCandidates, averageCandidates, majorityCandidates })
        {
            foreach (var kvp in task)
                results[kvp.Key] = kvp.Value;
        }

        return results;
    }
}