using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HiveMime.Controllers;

[ApiController]
[Route("api/poll")]
public class PollController(HiveMimeContext context) : ControllerBase
{
    [HttpGet("browse")]
    public List<ListPollDto> BrowsePolls()
    {
        List<Poll> polls = context.Polls
            .Include(p => p.Options)
            .Include(p => p.SubPolls)
            .Where(p => p.ParentPollId == null)
            .ToList();

        return polls.Select(p => p.ToListPollDto()).ToList();
    }

    [HttpPost("create")]
    public bool CreatePoll([FromBody] CreatePollDto pollDto)
    {
        context.Polls.Add(pollDto.ToPoll());

        return context.SaveChanges() > 0;
    }
}
