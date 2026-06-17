using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

public class PostService(HiveMimeContext context,
    HotnessUpdateQueue hotnessQueue,
    HoneyDeltaCalculator honeyDeltaCalculator,
    IMediaService mediaService,
    AuthorizationService authorizationService,
    HybridCache cache)
{
    /// <summary>
    /// Fetches and returns a post by its ID, including all its polls and candidates.
    /// </summary>
    /// <param name="postId">The ID of the post to fetch.</param>
    public async Task<PostDto> GetPostAsync(Guid postId)
    {
        return await cache.GetOrCreateAsync(CacheHelper.GetCacheKey([postId]), async entry =>
        {
            return await context.Posts
                .AsNoTracking()
                .QueryableFind(postId)
                .ProjectToType<PostDto>()
                .FirstOrExceptionAsync();
        });
    }

    /// <summary>
    /// Fetches and returns a pre selection of hot posts to show in the browse section.
    /// </summary>
    /// <param name="creatorId">The ID of the user to fetch posts from.</param>
    /// <param name="hiveId">The ID of the hive to fetch posts from.</param>
    /// <param name="pagination">The pagination parameters.</param>
    /// <param name="status">The approval status to filter posts by.</param>
    public async Task<PaginationResultDto<PostDto>> BrowsePostsAsync(Guid userId, Guid? creatorId, Guid? hiveId, PostPaginationDto pagination, ApprovalStatus status)
    {
        return await cache.GetOrCreateAsync(CacheHelper.GetCacheKey([creatorId, hiveId, pagination, status]), async entry =>
        {
            IQueryable<Post> posts = context.Posts
            .Where(p => !p.IsDraft && p.ApprovalStatus == status)
            .AsNoTracking();

            if (creatorId.HasValue)
                posts = posts.Where(p => p.CreatorId == creatorId.Value);
                
            if (hiveId.HasValue)
                posts = posts.Where(p => p.HiveId == hiveId.Value);

            if (status != ApprovalStatus.Approved)
            {
                if (!hiveId.HasValue)
                    throw new ValidationException("Hive ID must be provided for outstanding posts.");

                await authorizationService.VerifyApprovePostsAsync(userId, hiveId.Value);
            }
            else
            {
                posts = posts.Where(p => p.HiveId != null &&
                    (!p.Hive!.Settings.IsPrivate ||
                    p.Hive.Users.Any(f => f.UserId == userId && f.Role > MemberRole.Guest && f.ApprovalStatus == ApprovalStatus.Approved)));
            }

            var result = await new PostPaginationHelper(pagination)
                .ApplyPaginationAsync<PostDto>(posts);

            hotnessQueue.EnqueuePosts(result.Items.Select(p => p.Id));

            return result;
        });
    }

    /// <summary>
    /// Modifies the approval status of a post. Only users with the appropriate permissions can modify the status.
    /// </summary>
    /// <param name="userId">The ID of the user attempting to approve the post.</param>
    /// <param name="postId">The ID of the post to modify.</param>
    /// <param name="newStatus">The new approval status for the post.</param>
    /// <returns>The modified post.</returns>
    public async Task<PostDto> ModifyPostStatusAsync(Guid userId, Guid postId, ApprovalStatus newStatus)
    {
        await authorizationService.VerifyApprovePostAsync(userId, postId);
        Post post = await context.Posts.FirstOrExceptionAsync(p => p.Id == postId);

        post.ApprovalStatus = newStatus;
        await context.SaveChangesAsync();

        return await context.Posts.Where(p => p.Id == postId)
            .AsNoTracking()
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
    public async Task<HoneyDeltaDto<PostDto>> PublishPostAsync(Guid userId, Guid postId)
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

        var uploadedFiles = await mediaService.ListObjectsAsync($"posts/{post.Id}/");

        foreach (var uploadedFile in uploadedFiles)
        {
            string[] keyParts = uploadedFile.Split('/');

            if (keyParts.Length == 4)
            {
                Guid pollId = Guid.Parse(keyParts[2]);
                Poll poll = post.Polls.First(p => p.Id == pollId);

                poll.MediaKeys.Add(uploadedFile);
            }

            else if (keyParts.Length == 5)
            {
                Guid pollId = Guid.Parse(keyParts[2]);
                Guid candidateId = Guid.Parse(keyParts[3]);
                Poll poll = post.Polls.First(p => p.Id == pollId);
                Candidate candidate = poll.Candidates.FirstOrDefault(c => c.Id == candidateId);

                candidate.MediaKeys.Add(uploadedFile);
            }
        }

        if (post.HiveId is null || !post.Hive!.Settings.PostRequiresApproval)
            post.ApprovalStatus = ApprovalStatus.Approved;

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
    public async Task<UploadPostDto> CreatePostAsync(Guid userId, CreatePostDto postDto)
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

        foreach(var candidate in newPost.Polls.SelectMany(p => p.Candidates))
            candidate.NormalizedName = candidate.Name.Normalize(true);
            
        for (int i = 0; i < newPost.Polls.Count; i++)
        {
            newPost.Polls[i].Order = i;

            for (int j = 0; j < newPost.Polls[i].Candidates.Count; j++)
                newPost.Polls[i].Candidates[j].Order = j;
            
            for (int j = 0; j < newPost.Polls[i].Categories.Count; j++)
                newPost.Polls[i].Categories[j].Order = j;
        }

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
    public async Task DeletePostAsync(Guid userId, Guid postId)
    {
        await authorizationService.VerifyDeletePostAsync(userId, postId);

        Post post = await context.Posts.FirstOrExceptionAsync(p => p.Id == postId);

        context.Posts.Remove(post);

        await mediaService.DeleteObjectsAsync($"posts/{post.Id}/");
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Fetches and returns the sum result of a poll, which is the sum of the values of all votes for each candidate.
    /// </summary>
    /// <param name="pollId">The ID of the poll to fetch results for.</param>
    /// <param name="filter">The filter to apply to the poll results.</param>
    public async Task<PollResultDto<CandidateSumResultDto>> GetPollSumResult(Guid pollId, string filter)
    {
        var sumResults = await cache.GetOrCreateAsync(CacheHelper.GetCacheKey([pollId, filter]), async entry =>
        {
            IQueryable<CandidateVoteWithMetaData> candidateVotes = GetApplicableVotes(pollId, filter);

            return await candidateVotes
                .GroupBy(v => new { v.CandidateId, v.CandidateName, v.IsCustom })
                .Select(g => new CandidateSumResultDto
                {
                    Id = g.Key.CandidateId,
                    Name = g.Key.CandidateName,
                    IsCustom = g.Key.IsCustom,
                    Sum = g.Sum(v => v.Value),
                    VoteCount = g.Count()
                })
                .ToListAsync();
        });
        

        return ToPollResultDto(sumResults);
    }

    /// <summary>
    /// Fetches and returns a box-plot result of a poll, which includes minimum, first quartile, median, third quartile and maximum of the values of all votes for each candidate.
    /// </summary>
    /// <param name="pollId">The ID of the poll to fetch results for.</param>
    /// <param name="filter">The filter to apply to the poll results.</param>
    public async Task<PollResultDto<CandidateStatisticsResultDto>> GetPollStatisticsResult(Guid pollId, string filter)
    {
        var statisticsResults = await cache.GetOrCreateAsync(CacheHelper.GetCacheKey([pollId, filter]), async entry =>
        {
            IQueryable<CandidateVoteWithMetaData> candidateVotes = GetApplicableVotes(pollId, filter);

            string sql = candidateVotes.AsSingleQuery().ToQueryString();

            Dictionary<string, string> parameters = Regex.Matches(sql, @"-- (@\w+)=(.+)")
                .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value.TrimEnd());

            foreach (var param in parameters)
                sql = sql.Replace(param.Key, param.Value);

            return await context.Database.SqlQueryRaw<CandidateStatisticsResultDto>($"""
                SELECT 
                    "CandidateId" AS "Id",
                    "CandidateName" AS "Name",
                    "IsCustom" AS "IsCustom",
                    MIN("Value") AS "Min",
                    PERCENTILE_CONT(0.25) WITHIN GROUP (ORDER BY "Value") AS "Q1",
                    PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY "Value") AS "Median",
                    PERCENTILE_CONT(0.75) WITHIN GROUP (ORDER BY "Value") AS "Q3",
                    MAX("Value") AS "Max",
                    AVG("Value") AS "Average",
                    Count(*) AS "VoteCount"
                FROM ({sql}) AS "CandidateVotes"
                GROUP BY "CandidateId", "CandidateName", "IsCustom"
                """)
                .ToListAsync();
        });

        return ToPollResultDto(statisticsResults);
    }

    /// <summary>
    /// Fetches and returns a distribution result of a poll, which includes the frequency of each value for each candidate.
    /// </summary>
    /// <param name="pollId">The ID of the poll to fetch results for.</param>
    /// <param name="filter">The filter to apply to the poll results.</param>
    public async Task<PollResultDto<CandidateDistributionResultDto>> GetPollDistributionResult(Guid pollId, string filter)
    {
        var distributionResults = await cache.GetOrCreateAsync(CacheHelper.GetCacheKey([pollId, filter]), async entry =>
        {
            IQueryable<CandidateVoteWithMetaData> candidateVotes = GetApplicableVotes(pollId, filter);

            var distributionResults = await candidateVotes
                .GroupBy(v => new { v.CandidateId, v.Value, v.CandidateName, v.IsCustom })
                .Select(g => new
                {
                    g.Key.CandidateId,
                    g.Key.Value,
                    g.Key.CandidateName,
                    g.Key.IsCustom,
                    Count = g.Count()
                })
                .ToListAsync();

            var groupedResults = distributionResults
                .GroupBy(r => new { r.CandidateId, r.CandidateName, r.IsCustom })
                .Select(g => new CandidateDistributionResultDto
                {
                    Id = g.Key.CandidateId,
                    Name = g.Key.CandidateName,
                    IsCustom = g.Key.IsCustom,
                    VoteCount = g.Sum(r => r.Count),
                    Distribution = g.Select(r => new CandidationDistributionResultValueDto
                    {
                        Value = r.Value,
                        VoteCount = r.Count
                    }).ToList()
                })
                .ToList();

            return groupedResults;
        });

        return ToPollResultDto(distributionResults);
    }

    /// <summary>
    /// Inserts or updates a user's votes on a post.
    /// </summary>
    /// <param name="userId">The ID of the user voting.</param>
    /// <param name="vote">The vote to insert or update.</param>
    public async Task<HoneyDeltaDto<bool>> VoteOnPostAsync(Guid userId, PostVoteDto vote)
    {
        await authorizationService.VerifyVoteOnPostAsync(userId, vote.Id);
        
        Post post = await context.Posts
            .AsNoTracking()
            .Include(p => p.Polls)
            .FirstOrExceptionAsync(p => p.Id == vote.Id);

        IEnumerable<string> validationErrors = ValidatePostVotes(post, vote);

        if (validationErrors.Any())
            throw new ValidationException("Vote validation failed: " + string.Join("; ", validationErrors));
        
        var customCandidateIds = await GetOrCreateCustomCandidates(vote);

        HoneyDeltaDto<bool> honeyDelta = new() { Dto = true };

        PostVote postVote = await context.PostVotes
            .Include(pv => pv.Votes)
            .FirstOrDefaultAsync(pv => pv.UserId == userId && pv.PostId == vote.Id);
        
        if (postVote is not null)
            context.PostVotes.Remove(postVote);
        else
            honeyDelta = await honeyDeltaCalculator.FromPostVoteAsync(userId, vote);
            
        postVote = new PostVote
        {
            UserId = userId,
            PostId = post.Id,
            Votes = new List<CandidateVote>()
        };

        context.PostVotes.Add(postVote);
        HashSet<Guid> votedCandidateIds = new();

        foreach (PollVoteDto pollVote in vote.Polls)
        {
            foreach (CandidateVoteDto candidateVote in pollVote.Candidates)
            {
                if (!candidateVote.Value.HasValue)
                    continue;
                
                if (candidateVote.Id is null)
                    candidateVote.Id = customCandidateIds[(pollVote.Id, candidateVote.Name.Normalize(false))].Id;

                if (votedCandidateIds.Contains(candidateVote.Id.Value))
                    continue;

                var dbVote = new CandidateVote
                {
                    CandidateId = candidateVote.Id.Value,
                    PostVote = postVote
                };

                postVote.Votes.Add(dbVote);

                dbVote.Value = candidateVote.Value ?? 0;
                votedCandidateIds.Add(candidateVote.Id.Value);
            }
        }

        await context.SaveChangesAsync();

        return honeyDelta;
    }

    /// <summary>
    /// Modifies the approval status of a post.
    /// </summary>
    /// <param name="userId">The ID of the user attempting to modify the post.</param>
    /// <param name="postId">The ID of the post to modify.</param>
    /// <param name="approvalStatus">The new approval status for the post.</param>
    public async Task ModifyPostAsync(Guid userId, Guid postId, ApprovalStatus approvalStatus)
    {
        await authorizationService.VerifyApprovePostAsync(userId, postId);

        Post post = await context.Posts.FirstOrExceptionAsync(p => p.Id == postId);
        post.ApprovalStatus = approvalStatus;

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Returns a list of custom candidate suggestions for a poll based on a query string.
    /// Only candidates that start with the query string and are marked as custom will be returned.
    /// </summary>
    /// <param name="pollId">The ID of the poll for which to retrieve custom candidate suggestions.</param>
    /// <param name="query">The query string to filter custom candidates.</param>
    /// <returns>A list of <see cref="CandidateDto"/> representing the custom candidate suggestions.</returns>
    /// <exception cref="ValidationException">Thrown if the query is invalid or the poll does not allow custom answers.</exception>
    public async Task<List<CandidateDto>> GetCustomCandidateSuggestionsAsync(Guid pollId, string query)
    {
        string trimmedQuery = query.Normalize(false);

        if (string.IsNullOrWhiteSpace(trimmedQuery) || trimmedQuery.Length < 3)
            return [];

        return await cache.GetOrCreateAsync(CacheHelper.GetCacheKey([pollId, trimmedQuery]), async entry =>
        {
            bool? allowCustomCandidate = await context.Polls
            .AsNoTracking()
            .Where(p => p.Id == pollId)
            .Select(p => p.AllowedCustomCandidateCount > 0)
            .FirstOrDefaultAsync();

            if (allowCustomCandidate == null)
                throw new ValidationException("Poll not found.");

            if (!allowCustomCandidate.Value)
                throw new ValidationException("This poll does not allow custom candidates.");

            return await context.Candidates
                .Where(c => c.PollId == pollId)
                .Where(c => c.NormalizedName.StartsWith(trimmedQuery) && c.IsCustom)
                .OrderBy(c => c.Name)
                .Take(10)
                .ProjectToType<CandidateDto>()
                .ToListAsync();
        });
    }

    private async Task<Dictionary<(Guid, string), Candidate>> GetOrCreateCustomCandidates(PostVoteDto postVoteDto, int retryCount = 0)
    {
        HashSet<(Guid, string, string)> customCandidates = postVoteDto.Polls
            .SelectMany(p => p.Candidates.Where(c => !c.Id.HasValue && c.Value.HasValue)
                .Select(candidate => (p.Id, candidate.Name.Normalize(false), candidate.Name)))
            .ToHashSet();

        if (!customCandidates.Any())
            return [];

        IEnumerable<string> customCandidateNames = customCandidates.Select(c => c.Item2).Distinct();
        Dictionary<(Guid, string), Candidate> existingCandidates = 
            context.Candidates.Where(c => c.Poll.PostId == postVoteDto.Id)
                .AsNoTracking()
                .Where(c => c.IsCustom && customCandidateNames.Contains(c.NormalizedName))
                .ToDictionary(c => (c.PollId, c.NormalizedName), c => c);

        foreach (var customCandidate in customCandidates)
        {
            if (!existingCandidates.ContainsKey((customCandidate.Item1, customCandidate.Item2)))
            {
                Candidate newCandidate = new()
                {
                    PollId = customCandidate.Item1,
                    Name = customCandidate.Item3,
                    NormalizedName = customCandidate.Item2,
                    IsCustom = true
                };

                context.Candidates.Add(newCandidate);
                existingCandidates[(customCandidate.Item1, customCandidate.Item2)] = newCandidate;
            }
        }

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate key value violates unique constraint") == true)
        {
            if (retryCount >= 3)
                throw new ValidationException("Failed to create custom candidates after multiple attempts due to concurrent modifications. Please try again later.");

            context.ChangeTracker.Clear();

            return await GetOrCreateCustomCandidates(postVoteDto, retryCount + 1);
        }

        return existingCandidates;
    }

    private IQueryable<CandidateVoteWithMetaData> GetApplicableVotes(Guid pollId, string filter)
    {
        IQueryable<PostVote> votes = context.PostVotes
            .Where(v => v.Post.Polls.Any(p => p.Id == pollId));

        if (!string.IsNullOrWhiteSpace(filter))
        {
            VoteQueryBase voteQuery = filter.ToVoteQuery();
            Expression<Func<PostVote, bool>> voteExpression = voteQuery.ToExpression();
            votes = votes.Where(v => !v.User.Settings.ProtectVoteOnFilter)
                .Where(voteExpression);
        }

        return votes.SelectMany(v => v.Votes)
            .Where(cv => cv.Candidate.PollId == pollId)
            .Select(cv => new CandidateVoteWithMetaData
            {
                CandidateId = cv.CandidateId,
                Value = cv.Value,
                CandidateName = cv.Candidate.Name,
                IsCustom = cv.Candidate.IsCustom
            });
    }

    private PollResultDto<T> ToPollResultDto<T>(List<T> candidateResults, int customCount = 50) where T : CandidateResultDto
    {
        var customResults = candidateResults.Where(c => c.IsCustom)
            .OrderByDescending(c => c.VoteCount)
            .Take(customCount);

        var nonCustomResults = candidateResults.Where(c => !c.IsCustom);

        return new PollResultDto<T>
        {
            Candidates = nonCustomResults.Concat(customResults).ToList()
        };
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
                string objectKey = $"posts/{uploadPost.Id}/{uploadPoll.Id}/{Guid.NewGuid()}";
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
                    string objectKey = $"posts/{uploadPost.Id}/{uploadPoll.Id}/{uploadCandidate.Id}/{Guid.NewGuid()}";
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
        int effectiveCandidateCount = dto.Candidates.Count + dto.AllowedCustomCandidateCount;
        
        dto.MinVotes = Math.Clamp(dto.MinVotes, 0, effectiveCandidateCount);

        if (dto.MaxVotes == -1)
            dto.MaxVotes = effectiveCandidateCount;
        else
            dto.MaxVotes = Math.Clamp(dto.MaxVotes, dto.MinVotes, effectiveCandidateCount);

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

        if (effectiveCandidateCount == 0)
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

        foreach (Poll poll in post.Polls)
        {
            PollVoteDto? pollVote = postVote.Polls.First(pv => pv.Id == poll.Id);

            foreach (string error in ValidateVote(poll, pollVote!))
                yield return error;
        }
    }

    private IEnumerable<string> ValidateVote(Poll poll, PollVoteDto pollVote)
    {
        int votesCount = pollVote.Candidates.Count(v => v.Value.HasValue && (poll.PollType != PollType.Choice || v.Value.Value != 0));

        // General validation.
        if (votesCount < poll.MinVotes)
            yield return $"Poll requires at least {poll.MinVotes} votes.";

        if (votesCount > poll.MaxVotes)
            yield return $"Poll allows a maximum of {poll.MaxVotes} votes.";

        if (pollVote.Candidates.Any(v => v.Value.HasValue && v.Value < poll.MinValue))
            yield return $"Poll has a minimum value of {poll.MinValue}.";

        if (pollVote.Candidates.Any(v => v.Value.HasValue && v.Value > poll.MaxValue))
            yield return $"Poll has a maximum value of {poll.MaxValue}.";

        if (pollVote.Candidates.Count(c => c.Id is null && c.Value.HasValue) > poll.AllowedCustomCandidateCount)
            yield return $"Poll allows a maximum of {poll.AllowedCustomCandidateCount} custom candidates.";

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

        for (int rank = 1; rank <= expectedRankCount; rank++)
        {
            if (!uniqueRanks.Contains(rank))
                yield return $"Ranking poll is missing rank {rank}.";
        }
    }

    private class CandidateVoteWithMetaData
    {
        public Guid CandidateId { get; set; }
        public int Value { get; set; }
        public string CandidateName { get; set; }
        public bool IsCustom { get; set; }
    }
}