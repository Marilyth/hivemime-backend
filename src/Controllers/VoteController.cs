using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HiveMime.Controllers;

[ApiController]
[Route("api/post/{postId}")]
public class VoteController(GeoIPService geoIPService, IPostService postService, HiveMimeContext context) : ControllerBase
{
    [HttpGet]
    public PostResultsDto GetPostResults(int postId)
    {
        return postService.GetPostDetails(postId);
    }

    [HttpPost("vote")]
    [Authorize]
    public async Task UpsertVoteToPost([FromBody] UpsertVoteToPostDto vote)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        string country = await geoIPService.GetCountryOfIPAsync(ipAddress);

        postService.UpsertVoteToPost(User.GetUserId(), vote, country);
    }
}
