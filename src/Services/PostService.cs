using System.Linq.Expressions;
using Mapster;
using Microsoft.EntityFrameworkCore;

public class PostService(HiveMimeContext context,
    HotnessUpdateQueue hotnessQueue,
    HoneyDeltaCalculator honeyDeltaCalculator,
    IMediaService mediaService,
    AuthorizationService authorizationService)
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
    /// <param name="hiveId">The ID of the hive to fetch posts from.</param>
    /// <param name="pagination">The pagination parameters.</param>
    /// <param name="onlyOutstanding">Whether to fetch only outstanding posts.</param>
    public async Task<PaginationResultDto<PostDto>> BrowsePostsAsync(int userId, int? creatorId, int? hiveId, PostPaginationDto pagination, bool onlyOutstanding = false)
    {
        IQueryable<Post> posts = context.Posts
            .Where(p => !p.IsDraft)
            .AsNoTracking();

        if (creatorId.HasValue)
            posts = posts.Where(p => p.CreatorId == creatorId.Value);
            
        if (hiveId.HasValue)
            posts = posts.Where(p => p.HiveId == hiveId.Value);

        if (onlyOutstanding)
        {
            if (!hiveId.HasValue)
                throw new ValidationException("Hive ID must be provided for outstanding posts.");

            await authorizationService.VerifyApprovePostsAsync(userId, hiveId.Value);
            posts = posts.Where(p => !p.IsApproved);
        }
        else
        {
            posts = posts.Where(p => p.IsApproved &&
                p.HiveId != null && (!p.Hive!.Settings.IsPrivate || p.Hive.Followers.Any(f => f.UserId == userId && f.IsApproved)));
        }

        var result = await posts.ApplyPaginationFilter(pagination)
            .ApplyPaginationOrdering(pagination)
            .ApplyPaginationPageSize(pagination)
            .FetchPaginationResultAsync(pagination);

        hotnessQueue.EnqueuePosts(result.Items.Select(p => p.Id));

        return result;
    }

    /// <summary>
    /// Approves a post, making it visible to other users if it was not already. Only users with the appropriate permissions can approve a post.
    /// </summary>
    /// <param name="userId">The ID of the user attempting to approve the post.</param>
    /// <param name="postId">The ID of the post to approve.</param>
    /// <returns>The approved post.</returns>
    public async Task<PostDto> ApprovePostAsync(int userId, int postId)
    {
        await authorizationService.VerifyApprovePostAsync(userId, postId);
        IQueryable<Post> postQuery = context.Posts.Where(p => p.Id == postId);

        await postQuery.ExecuteUpdateAsync(p => p.SetProperty(p => p.IsApproved, true));

        return await postQuery.AsNoTracking()
            .ProjectToType<PostDto>()
            .FirstAsync();
    }

    /// <summary>
    /// Publishes a post, marking it as no longer a draft and linking any uploaded media to it.
    /// If the hive the post belongs to does not require approval to post, the post will be approved as well.
    /// </summary>
    /// <param name="userId">The ID of the user attempting to publish the post.</param>
    /// <param name="postId">The ID of the post to be published.</param>
    /// <returns>True if the post was successfully published; otherwise, false.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if the user is not the creator of the post.</exception>
    /// <exception cref="ValidationException">Thrown if the post is already published.</exception>
    public async Task<HoneyDeltaDto<PostDto>> PublishPostAsync(int userId, int postId)
    {
        Post post = await context.Posts
            .Include(p => p.Polls)
                .ThenInclude(p => p.Candidates)
            .Include(p => p.Hive)
                .ThenInclude(h => h.Settings)
            .FirstOrExceptionAsync(p => p.Id == postId);

        if (post.CreatorId != userId)
            throw new UnauthorizedAccessException("You are not the creator of this post.");

        if (!post.IsDraft)
            throw new ValidationException("Post is already published.");

        post.IsDraft = false;

        var uploadedFiles = await mediaService.ListObjectsAsync($"{post.Id}/");

        foreach (var uploadedFile in uploadedFiles)
        {
            string[] keyParts = uploadedFile.Split('/');

            if (keyParts.Length == 3)
            {
                int pollId = int.Parse(keyParts[1]);
                Poll poll = post.Polls.First(p => p.Id == pollId);

                poll.MediaKeys.Add(uploadedFile);
            }

            else if (keyParts.Length == 4)
            {
                int pollId = int.Parse(keyParts[1]);
                int candidateId = int.Parse(keyParts[2]);
                Poll poll = post.Polls.First(p => p.Id == pollId);
                Candidate candidate = poll.Candidates.First(c => c.Id == candidateId);

                candidate.MediaKeys.Add(uploadedFile);
            }
        }

        if (post.HiveId is null || !post.Hive!.Settings.MustBeApprovedToPost)
            post.IsApproved = true;

        await context.SaveChangesAsync();
        
        PostDto postDto = await context.Posts.Where(p => p.Id == postId)
            .ProjectToType<PostDto>()
            .FirstAsync();

        return await honeyDeltaCalculator.FromPostDtoAsync(userId, postDto);
    }

    /// <summary>
    /// Creates and returns a new post based on the provided data. The post will be unpublished for further review.
    /// </summary>
    /// <param name="userId">The ID of the user creating the post.</param>
    /// <param name="postDto">The post to create.</param>
    public async Task<UploadPostDto> CreatePostAsync(int userId, CreatePostDto postDto)
    {
        IEnumerable<string> validationErrors = ValidateCreatePost(postDto);
        Hive hive = null;

        if (postDto.HiveId.HasValue)
            hive = await context.Hives.FirstOrExceptionAsync(h => h.Id == postDto.HiveId.Value);

        if (validationErrors.Any())
            throw new ValidationException("Post validation failed: " + string.Join("; ", validationErrors));

        await authorizationService.VerifyCreatePostAsync(userId, postDto.HiveId);

        Post newPost = postDto.Adapt<Post>();
        newPost.IsDraft = true;

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

        UploadPostDto uploadPostDto = await CreateUploadPostDto(postDto, newPost);

        return uploadPostDto;
    }

    /// <summary>
    /// Deletes a post. Also deletes any media associated with the post from Cloudflare R2.
    /// </summary>
    /// <param name="userId">The ID of the user attempting to delete the post.</param>
    /// <param name="postId">The ID of the post to delete.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown if the user is not authorized to delete the post.</exception>
    public async Task DeletePostAsync(int userId, int postId)
    {
        await authorizationService.VerifyDeletePostAsync(userId, postId);

        Post post = await context.Posts.FirstOrExceptionAsync(p => p.Id == postId);

        context.Posts.Remove(post);

        await mediaService.DeleteObjectsAsync($"{post.Id}/");
        await context.SaveChangesAsync();
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

        IQueryable<PostVote> filteredVotes = context.PostVotes
            .Where(v => v.PostId == postId);

        if (!string.IsNullOrWhiteSpace(filter))
        {
            VoteQueryBase voteQuery = filter.ToVoteQuery();
            Expression<Func<PostVote, bool>> voteExpression = voteQuery.ToExpression();
            filteredVotes = filteredVotes.Where(v => !v.User.Settings.ProtectVoteOnFilter)
                .Where(voteExpression);
        }

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
    public async Task<HoneyDeltaDto<bool>> VoteOnPostAsync(int userId, PostVoteDto vote)
    {
        Post post = await context.Posts
            .Include(p => p.Polls.OrderBy(p => p.Id))
                .ThenInclude(o => o.Candidates.OrderBy(c => c.Id))
            .Include(p => p.Polls.OrderBy(p => p.Id))
                .ThenInclude(o => o.Categories.OrderBy(c => c.Id))
            .FirstAsync(p => p.Id == vote.PostId);

        IEnumerable<string> validationErrors = ValidatePostVotes(post, vote);

        if (validationErrors.Any())
            throw new ValidationException("Vote validation failed: " + string.Join("; ", validationErrors));
            
        HoneyDeltaDto<bool> honeyDelta = new() { Dto = true };

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
            honeyDelta = await honeyDeltaCalculator.FromPostVoteAsync(userId, vote);
        }

        foreach ((Poll poll, PollVoteDto pollVote) in post.Polls.Zip(vote.Polls))
        {
            foreach ((Candidate candidate, CandidateVoteDto candidateVote) in poll.Candidates.Zip(pollVote.Candidates))
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

        return honeyDelta;
    }

    /// <summary>
    /// Generates pre-signed upload URLs for the media files associated with a post's polls and candidates,
    /// allowing the client to upload files directly to Cloudflare R2. Validates the total content length
    /// of the files to ensure it does not exceed the allowed limit.
    /// </summary>
    /// <param name="userId">The ID of the user requesting the upload URLs.</param>
    /// <param name="postDto">The post for which to generate upload URLs.</param>
    /// <returns>An <see cref="UploadPostDto"/> containing the pre-signed upload URLs.</returns>
    /// <exception cref="ValidationException">Thrown if the post does not exist or is already published.</exception>
    private async Task<UploadPostDto> CreateUploadPostDto(CreatePostDto postDto, Post post)
    {
        UploadPostDto uploadPost = post.Adapt<UploadPostDto>();
        ulong totalContentLength = 0;

        for (int i = 0; i < postDto.Polls.Count; i++)
        {
            var poll = postDto.Polls[i];
            var uploadPoll = uploadPost.Polls[i];
            
            if (poll.Media is not null)
            {
                string objectKey = $"{uploadPost.Id}/{uploadPoll.Id}/{Guid.NewGuid()}";
                string signedUploadUrl = mediaService.GetPreSignedURL(objectKey, poll.Media.ContentLength, poll.Media.ContentType);
                string signedThumbnailUploadUrl = mediaService.GetPreSignedURL(objectKey + "_thumb", poll.Media.ThumbnailContentLength, poll.Media.ContentType);

                uploadPoll.MediaUploadUrls = [signedUploadUrl, signedThumbnailUploadUrl];
                totalContentLength += poll.Media.ContentLength;
                totalContentLength += poll.Media.ThumbnailContentLength;
            }

            for (int j = 0; j < poll.Candidates.Count; j++)
            {
                var candidate = poll.Candidates[j];
                var uploadCandidate = uploadPoll.Candidates[j];

                if (candidate.Media is not null)
                {
                    string objectKey = $"{uploadPost.Id}/{uploadPoll.Id}/{uploadCandidate.Id}/{Guid.NewGuid()}";
                    string signedUploadUrl = mediaService.GetPreSignedURL(objectKey, candidate.Media.ContentLength, candidate.Media.ContentType);
                    string signedThumbnailUploadUrl = mediaService.GetPreSignedURL(objectKey + "_thumbnail", candidate.Media.ThumbnailContentLength, candidate.Media.ContentType);

                    uploadCandidate.MediaUploadUrls = new List<string> { signedUploadUrl, signedThumbnailUploadUrl };
                    totalContentLength += candidate.Media.ContentLength;
                    totalContentLength += candidate.Media.ThumbnailContentLength;
                }
            }
        }

        if (totalContentLength > CloudflareR2Service.MaxTotalSize)
            throw new ValidationException($"Total content length cannot exceed {CloudflareR2Service.MaxTotalSize} bytes.");

        return uploadPost;
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
                throw new ValidationException("StepValue must be set for scoring polls.");

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

    private IEnumerable<string> ValidatePostVotes(Post post, PostVoteDto postVote)
    {
        if (post.Polls.Count != postVote.Polls.Count)
        {
            yield return "The number of polls voted on does not match the number of polls in the post.";
            yield break;
        }

        foreach ((Poll poll, PollVoteDto pollVote) in post.Polls.Zip(postVote.Polls))
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

    private IEnumerable<string> ValidateVote(Poll poll, PollVoteDto pollVote)
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

    private IEnumerable<string> ValidateRankingPoll(Poll poll, PollVoteDto pollVote)
    {
        List<int> assignedRanks = pollVote.Candidates
            .Where(v => v.Value.HasValue)
            .Select(v => v.Value!.Value)
            .ToList();

        int expectedRankCount = assignedRanks.Count;
        HashSet<int> uniqueRanks = new(assignedRanks);

        if (uniqueRanks.Count != expectedRankCount)
            yield return "Duplicate values are not allowed in ranking polls.";

        for (int rank = poll.MaxValue - expectedRankCount + 1; rank <= poll.MaxValue; rank++)
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