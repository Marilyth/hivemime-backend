using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HiveMime.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostController(PostService postService,
    PostVoteService postVoteService,
    PostResultService postResultService,
    HiveMimeContext context) : ControllerBase
{
    [HttpGet("get")]
    [EnableRateLimiting("1/1s")]
    public async Task<PostDto> GetPostById(Guid postId)
        => await postService.GetPostAsync(postId);

    [HttpPost("browse")]
    [EnableRateLimiting("1/1s")]
    public async Task<PaginationResultDto<PostDto>> BrowsePosts(Guid? creatorId, Guid? hiveId, PostPaginationDto pagination, ApprovalStatus approvalStatus)
        => await postService.BrowsePostsAsync(await User.GetUserIdAsync(context), creatorId, hiveId, pagination, approvalStatus);

    [HttpPatch("publish")]
    [EnableRateLimiting("1/1m")]
    public async Task<HoneyDeltaDto<PostDto>> PublishPost(Guid postId)
        => await postService.PublishPostAsync(await User.GetUserIdAsync(context), postId);

    [HttpPost("create")]
    [EnableRateLimiting("1/1m")]
    public async Task<UploadPostDto> CreatePost([FromBody] CreatePostDto postDto)
        => await postService.CreatePostAsync(await User.GetUserIdAsync(context), postDto);

    [HttpDelete("delete")]
    [EnableRateLimiting("1/5s")]
    public async Task DeletePost(Guid postId)
        => await postService.DeletePostAsync(await User.GetUserIdAsync(context), postId);

    [HttpPatch("modifyPost")]
    [EnableRateLimiting("1/5s")]
    public async Task ModifyPost(Guid postId, ApprovalStatus approvalStatus)
        => await postService.ModifyPostAsync(await User.GetUserIdAsync(context), postId, approvalStatus);

    [HttpGet("choiceResult")]
    [EnableRateLimiting("5/5s")]
    public async Task<PollResultDto<CandidateChoiceResultDto>> GetChoicePollResult(Guid pollId, string? filter)
        => await postResultService.GetChoicePollResult(pollId, filter);

    [HttpGet("scoreResult")]
    [EnableRateLimiting("5/5s")]
    public async Task<PollResultDto<CandidateScoreResultDto>> GetScorePollResult(Guid pollId, string? filter)
        => await postResultService.GetScorePollResult(pollId, filter);

    [HttpGet("rankResult")]
    [EnableRateLimiting("5/5s")]
    public async Task<PollResultDto<CandidateRankResultDto>> GetRankPollResult(Guid pollId, string? filter)
        => await postResultService.GetRankPollResult(pollId, filter);

    [HttpGet("categoryResult")]
    [EnableRateLimiting("5/5s")]
    public async Task<PollResultDto<CandidateCategoryResultDto>> GetCategoryPollResult(Guid pollId, string? filter)
        => await postResultService.GetCategoryPollResult(pollId, filter);

    [HttpGet("locateResult")]
    [EnableRateLimiting("5/5s")]
    public async Task<PollResultDto<CandidateLocateResultDto>> GetLocatePollResult(Guid pollId, string? filter)
        => await postResultService.GetLocatePollResult(pollId, filter);

    [HttpGet("customCandidateSuggestions")]
    [EnableRateLimiting("5/1s")]
    public async Task<List<CandidateDto>> GetCustomCandidateSuggestions(Guid pollId, string query)
        => await postVoteService.GetCustomCandidateSuggestionsAsync(pollId, query);

    [HttpPost("vote")]
    [EnableRateLimiting("1/5s")]
    public async Task<HoneyDeltaDto<bool>> UpsertVoteToPost([FromBody] PostVoteDto vote)
        => await postVoteService.VoteOnPostAsync(await User.GetUserIdAsync(context), vote);
}
