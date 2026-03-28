using Microsoft.AspNetCore.Mvc;

namespace HiveMime.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostController(PostService postService, GeoIPService geoIPService) : ControllerBase
{
    [HttpGet("get")]
    public async Task<PostDto> GetPostById(int postId)
        => await postService.GetPostAsync(postId);

    [HttpGet("browse")]
    public async Task<List<PostDto>> BrowsePosts(int? afterId, int? hiveId, string? filter)
        => await postService.BrowsePostsAsync(User.GetUserId(), afterId, hiveId, filter);

    [HttpPost("create")]
    public async Task<PostDto> CreatePost([FromBody] CreatePostDto postDto)
        => await postService.CreatePostAsync(User.GetUserId(), postDto);

    [HttpGet("results")]
    public async Task<PostResultDto> GetPostResults(int postId, string? filter)
        => await postService.GetPostResultAsync(postId, filter);

    [HttpPost("vote")]
    public async Task UpsertVoteToPost([FromBody] VoteOnPostDto vote)
        => await postService.VoteOnPostAsync(User.GetUserId(), vote);
}
