using Microsoft.AspNetCore.Mvc;

namespace HiveMime.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostController(PostService postService, HiveMimeContext context) : ControllerBase
{
    [HttpGet("get")]
    public async Task<PostDto> GetPostById(Guid postId)
        => await postService.GetPostAsync(postId);

    [HttpPost("browse")]
    public async Task<PaginationResultDto<PostDto>> BrowsePosts(Guid? creatorId, Guid? hiveId, PostPaginationDto pagination, ApprovalStatus approvalStatus)
        => await postService.BrowsePostsAsync(await User.GetUserIdAsync(context), creatorId, hiveId, pagination, approvalStatus);

    [HttpPatch("publish")]
    public async Task<HoneyDeltaDto<PostDto>> PublishPost(Guid postId)
        => await postService.PublishPostAsync(await User.GetUserIdAsync(context), postId);

    [HttpPost("create")]
    public async Task<UploadPostDto> CreatePost([FromBody] CreatePostDto postDto)
        => await postService.CreatePostAsync(await User.GetUserIdAsync(context), postDto);

    [HttpDelete("delete")]
    public async Task DeletePost(Guid postId)
        => await postService.DeletePostAsync(await User.GetUserIdAsync(context), postId);

    [HttpPatch("modifyPost")]
    public async Task ModifyPost(Guid postId, ApprovalStatus approvalStatus)
        => await postService.ModifyPostAsync(await User.GetUserIdAsync(context), postId, approvalStatus);

    [HttpGet("sumResult")]
    public async Task<PollResultDto<CandidateSumResultDto>> GetPollSumResult(Guid pollId, string? filter)
        => await postService.GetPollSumResult(pollId, filter);

    [HttpGet("statisticsResult")]
    public async Task<PollResultDto<CandidateStatisticsResultDto>> GetPollStatisticsResult(Guid pollId, string? filter)
        => await postService.GetPollStatisticsResult(pollId, filter);

    [HttpGet("distributionResult")]
    public async Task<PollResultDto<CandidateDistributionResultDto>> GetPollDistributionResult(Guid pollId, string? filter)
        => await postService.GetPollDistributionResult(pollId, filter);

    [HttpPost("vote")]
    public async Task<HoneyDeltaDto<bool>> UpsertVoteToPost([FromBody] PostVoteDto vote)
        => await postService.VoteOnPostAsync(await User.GetUserIdAsync(context), vote);
}
