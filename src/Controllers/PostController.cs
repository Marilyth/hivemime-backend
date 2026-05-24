using Microsoft.AspNetCore.Mvc;

namespace HiveMime.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostController(PostService postService, HiveMimeContext context) : ControllerBase
{
    [HttpGet("get")]
    public async Task<PostDto> GetPostById(int postId)
        => await postService.GetPostAsync(postId);

    [HttpPost("browseOutstanding")]
    public async Task<PaginationResultDto<PostDto>> BrowseOutstandingPosts(int hiveId, PostPaginationDto pagination)
        => await postService.BrowsePostsAsync(await User.GetUserIdAsync(context), null, hiveId, pagination, true);

    [HttpPost("browse")]
    public async Task<PaginationResultDto<PostDto>> BrowsePosts(int? creatorId, int? hiveId, PostPaginationDto pagination)
        => await postService.BrowsePostsAsync(await User.GetUserIdAsync(context), creatorId, hiveId, pagination, false);

    [HttpPatch("publish")]
    public async Task<HoneyDeltaDto<PostDto>> PublishPost(int postId)
        => await postService.PublishPostAsync(await User.GetUserIdAsync(context), postId);

    [HttpPost("create")]
    public async Task<UploadPostDto> CreatePost([FromBody] CreatePostDto postDto)
        => await postService.CreatePostAsync(await User.GetUserIdAsync(context), postDto);

    [HttpDelete("delete")]
    public async Task DeletePost(int postId)
        => await postService.DeletePostAsync(await User.GetUserIdAsync(context), postId);

    [HttpGet("results")]
    public async Task<PostResultDto> GetPostResults(int postId, string? filter)
        => await postService.GetPostResultAsync(postId, filter);

    [HttpGet("distribution")]
    public async Task<List<CandidateDistributionDto>> GetCandidateResult(int candidateId, string? filter)
        => await postService.GetCandidateDistributionResultsAsync(candidateId, filter);

    [HttpPut("vote")]
    public async Task<HoneyDeltaDto<bool>> UpsertVoteToPost([FromBody] PostVoteDto vote)
        => await postService.VoteOnPostAsync(await User.GetUserIdAsync(context), vote);
}
