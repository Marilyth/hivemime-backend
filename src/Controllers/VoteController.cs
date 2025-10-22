using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HiveMime.Controllers;

[ApiController]
[Route("api/post/{postId}")]
public class VoteController(IPostService postService, HiveMimeContext context) : ControllerBase
{
    [HttpGet]
    public PostResultsDto GetPostResults(int postId)
    {
        return postService.GetPostDetails(postId);
    }

    [HttpPost("vote")]
    [Authorize]
    public void UpsertVoteToPost([FromBody] UpsertVoteToPostDto vote)
    {
        postService.UpsertVoteToPost(User.GetUserId(), vote);
    }
}
