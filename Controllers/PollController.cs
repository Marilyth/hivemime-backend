using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HiveMime.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PollController : ControllerBase
{
    private HiveMimeContext _context;

    public PollController(HiveMimeContext context)
    {
        _context = context;
    }

    [HttpGet("get")]
    public GetPollDto GetPoll(int pollId)
    {
        Poll poll = _context.Polls
            .Include(p => p.Options)
            .Include(p => p.Demographics)
            .FirstOrDefault(p => p.Id == pollId);

        return poll?.ToPollOverviewDto();
    }

    [HttpPost("create")]
    public bool CreatePoll([FromBody] CreatePollDto pollDto)
    {
        _context.Polls.Add(pollDto.ToPoll());

        return _context.SaveChanges() > 0;
    }
}
