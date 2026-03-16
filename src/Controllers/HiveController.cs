using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HiveMime.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HiveController(IHiveService hiveService) : ControllerBase
{
    [HttpGet("get")]
    public HiveDto GetHiveById(int hiveId)
        => hiveService.GetHiveById(hiveId);

    [HttpGet("browse")]
    public List<HiveDto> BrowseHives(int? afterId, string? filter)
        => hiveService.BrowseHives(afterId, filter);

    [HttpPost("create")]
    [Authorize]
    public HiveDto CreateHive([FromBody] CreateHiveDto hiveDto)
        => hiveService.CreateHive(User.GetUserId(), hiveDto);
}
