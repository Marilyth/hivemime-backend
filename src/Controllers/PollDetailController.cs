using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HiveMime.Controllers;

[ApiController]
[Route("api/poll/{pollId}")]
public class PollDetailController(IPostService postService, HiveMimeContext context) : ControllerBase
{
    [HttpGet]
    public PollResultsDto GetPollResults(int pollId)
    {
        return postService.GetPollDetails(pollId);
    }

    [HttpPost("vote")]
    [Authorize]
    public void UpsertVoteToPost([FromBody] UpsertVoteToPostDto vote)
    {
        postService.UpsertVoteToPost(User.GetUserId(), vote);
    }
}
