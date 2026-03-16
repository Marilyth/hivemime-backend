using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HiveMime.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostController(IPostService postService, GeoIPService geoIPService) : ControllerBase
{
    [HttpGet("get")]
    public PostDto GetPostById(int postId)
        => postService.GetPostById(postId);

    [HttpGet("browse")]
    public List<PostDto> BrowsePosts(int? afterId, int? hiveId, string? filter)
        => postService.BrowsePosts(User.GetUserId(), afterId, hiveId, filter);

    [HttpPost("create")]
    [Authorize]
    public PostDto CreatePost([FromBody] CreatePostDto postDto)
        => postService.CreatePost(User.GetUserId(), postDto);

    [HttpGet("results")]
    public PostResultDto GetPostResults(int postId, string? filter)
        => postService.GetPostResult(postId, filter);

    [HttpPost("vote")]
    [Authorize]
    public async Task UpsertVoteToPost([FromBody] VoteOnPostDto vote)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        string country = await geoIPService.GetCountryOfIPAsync(ipAddress);

        postService.VoteOnPost(User.GetUserId(), vote, country);
    }
}
