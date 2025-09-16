using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HiveMime.Controllers;

[ApiController]
[Route("api/poll/{pollId}")]
public class PollDetailController(HiveMimeContext context) : ControllerBase
{
    [HttpGet]
    public PollResultsDto GetPollDetails(int pollId)
    {
        Poll poll = context.Polls
            .Include(p => p.Options)
                .ThenInclude(o => o.Votes)
            .First(p => p.Id == pollId);

        return poll.ToPollResultsDto();
    }

    [HttpPost("vote")]
    public bool AddVoteToPoll(int pollId, [FromBody] string vote)
    {
        return true;
    }
}
