using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HiveMime.Controllers;

[ApiController]
[Route("api/poll")]
public class PollController(IPollService pollService) : ControllerBase
{
    [HttpGet("browse")]
    public List<ListPollDto> BrowsePolls()
    {
        return pollService.BrowsePolls();
    }

    [HttpPost("create")]
    [Authorize]
    public void CreatePoll([FromBody] CreatePollDto pollDto)
    {
        pollService.CreatePoll(User.GetUserId(), pollDto);
    }
}
