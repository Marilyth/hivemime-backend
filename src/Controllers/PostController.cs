using Microsoft.AspNetCore.Mvc;

namespace HiveMime.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostController(PostService postService, HiveMimeContext context) : ControllerBase
{
    [HttpGet("get")]
    public async Task<PostDto> GetPostById(int postId)
        => await postService.GetPostAsync(postId);

    [HttpPost("browse")]
    public async Task<List<PostDto>> BrowsePosts(int? creatorId, int? hiveId, PostPaginationDto pagination)
        => await postService.BrowsePostsAsync(creatorId, hiveId, pagination);

    [HttpPost("create")]
    public async Task<PostDto> CreatePost([FromBody] CreatePostDto postDto)
        => await postService.CreatePostAsync(await User.GetUserIdAsync(context), postDto);

    [HttpGet("results")]
    public async Task<PostResultDto> GetPostResults(int postId, string? filter)
        => await postService.GetPostResultAsync(postId, filter);

    [HttpGet("distribution")]
    public async Task<List<CandidateDistributionDto>> GetCandidateResult(int candidateId, string? filter)
        => await postService.GetCandidateDistributionResultsAsync(candidateId, filter);

    [HttpPost("vote")]
    public async Task UpsertVoteToPost([FromBody] VoteOnPostDto vote)
        => await postService.VoteOnPostAsync(await User.GetUserIdAsync(context), vote);
}
