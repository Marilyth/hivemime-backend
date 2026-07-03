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
        return await cache.GetOrCreateAsync(CacheHelper.GetCacheKey([userId, creatorId, hiveId, pagination, status]), async entry =>
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
        await CheckForMediaAsync(post);

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

    private async Task CheckForMediaAsync(Post post)
    {
        bool hasMedia = post.Polls.Any(p => p.MediaKeys.Any() || p.Candidates.Any(c => c.MediaKeys.Any()));

        if (!hasMedia)
            return;

        HashSet<string> uploadedFiles = [..await mediaService.ListObjectsAsync($"posts/{post.Id}/")];

        foreach (var poll in post.Polls)
        {
            foreach (var mediaKey in poll.MediaKeys.ToList())
            {
                if (!uploadedFiles.Contains(mediaKey))
                    poll.MediaKeys.Remove(mediaKey);
            }

            foreach (var candidate in poll.Candidates)
            {
                foreach (var mediaKey in candidate.MediaKeys.ToList())
                {
                    if (!uploadedFiles.Contains(mediaKey))
                        candidate.MediaKeys.Remove(mediaKey);
                }
            }
        }
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
            var poll = post.Polls[i];
            var pollDto = postDto.Polls[i];
            var uploadPoll = uploadPost.Polls[i];
            
            if (pollDto.Media is not null)
            {
                string objectKey = $"posts/{uploadPost.Id}/{uploadPoll.Id}/{Guid.NewGuid()}";
                string signedUploadUrl = mediaService.GetPreSignedURL(objectKey,
                    pollDto.Media.ContentLength,
                    pollDto.Media.ContentType,
                    out string finalKey);
                string signedThumbnailUploadUrl = mediaService.GetPreSignedURL(objectKey + "_thumb",
                    pollDto.Media.ThumbnailContentLength,
                    pollDto.Media.ContentType,
                    out string finalThumbnailKey);

                poll.MediaKeys.Add(finalKey);
                poll.MediaKeys.Add(finalThumbnailKey);

                uploadPoll.MediaUploadUrls = [signedUploadUrl, signedThumbnailUploadUrl];
                totalContentLength += pollDto.Media.ContentLength;
                totalContentLength += pollDto.Media.ThumbnailContentLength;
            }

            for (int j = 0; j < pollDto.Candidates.Count; j++)
            {
                var candidate = poll.Candidates[j];
                var candidateDto = pollDto.Candidates[j];
                var uploadCandidate = uploadPoll.Candidates[j];

                if (candidateDto.Media is not null)
                {
                    string objectKey = $"posts/{uploadPost.Id}/{uploadPoll.Id}/{uploadCandidate.Id}/{Guid.NewGuid()}";
                    string signedUploadUrl = mediaService.GetPreSignedURL(objectKey,
                        candidateDto.Media.ContentLength,
                        candidateDto.Media.ContentType,
                        out string finalKey);
                    string signedThumbnailUploadUrl = mediaService.GetPreSignedURL(objectKey + "_thumbnail",
                        candidateDto.Media.ThumbnailContentLength,
                        candidateDto.Media.ContentType,
                        out string finalThumbnailKey);

                    candidate.MediaKeys.Add(finalKey);
                    candidate.MediaKeys.Add(finalThumbnailKey);

                    uploadCandidate.MediaUploadUrls = new List<string> { signedUploadUrl, signedThumbnailUploadUrl };
                    totalContentLength += candidateDto.Media.ContentLength;
                    totalContentLength += candidateDto.Media.ThumbnailContentLength;
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
}