using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HiveMime.Controllers;

[ApiController]
[Route("api/post")]
public class PostController(IPostService postService, GeoIPService geoIPService) : ControllerBase
{
    [HttpGet("browse")]
    public List<PostDto> BrowsePosts(int? afterId, string? filter)
    {
        return postService.BrowsePosts(User.GetUserId(), afterId, filter);
    }

    [HttpPost("create")]
    [Authorize]
    public void CreatePost([FromBody] CreatePostDto postDto)
    {
        postService.CreatePost(User.GetUserId(), postDto);
    }

    [HttpGet("results")]
    public PostResultDto GetPostResults(int postId, string? filter)
    {
        return postService.GetPostResult(postId, filter);
    }

    [HttpPost("vote")]
    [Authorize]
    public async Task UpsertVoteToPost([FromBody] VoteOnPostDto vote)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        string country = await geoIPService.GetCountryOfIPAsync(ipAddress);

        postService.VoteOnPost(User.GetUserId(), vote, country);
    }
}
