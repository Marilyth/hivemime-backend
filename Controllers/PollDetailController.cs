using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HiveMime.Controllers;

[ApiController]
[Route("api/poll/{pollId}")]
public class PollDetailController(IPollService pollService, HiveMimeContext context) : ControllerBase
{
    [HttpGet]
    public PollResultsDto GetPollDetails(int pollId)
    {
        return pollService.GetPollDetails(pollId);
    }

    [HttpPost("vote")]
    [Authorize]
    public void UpsertVoteToPoll([FromBody] UpsertVoteToPollDto[] votes)
    {
        pollService.UpsertVoteToPoll(User.GetUserId(), votes);
    }
}
