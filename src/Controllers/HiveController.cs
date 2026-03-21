using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HiveMime.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HiveController(HiveService hiveService) : ControllerBase
{
    [HttpGet("get")]
    public async Task<HiveDto> GetHiveById(int hiveId)
        => await hiveService.GetHiveAsync(hiveId);

    [HttpGet("followed")]
    [Authorize]
    public async Task<List<HiveDto>> GetFollowedHives()
        => await hiveService.GetFollowedHivesAsync(User.GetUserId());

    [HttpPost("join")]
    [Authorize]
    public async Task JoinHive(int hiveId)
        => await hiveService.JoinHiveAsync(User.GetUserId(), hiveId);
        
    [HttpPost("leave")]
    [Authorize]
    public async Task LeaveHive(int hiveId)
        => await hiveService.LeaveHiveAsync(User.GetUserId(), hiveId);

    [HttpGet("browse")]
    public async Task<List<HiveDto>> BrowseHives(int? afterId, string? filter)
        => await hiveService.BrowseHivesAsync(afterId, filter);

    [HttpPost("create")]
    [Authorize]
    public async Task<HiveDto> CreateHive([FromBody] CreateHiveDto hiveDto)
        => await hiveService.CreateHiveAsync(User.GetUserId(), hiveDto);
}
