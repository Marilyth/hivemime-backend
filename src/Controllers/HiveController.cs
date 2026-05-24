using Microsoft.AspNetCore.Mvc;

namespace HiveMime.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HiveController(HiveService hiveService, HiveMimeContext context) : ControllerBase
{
    [HttpGet("get")]
    public async Task<HiveDto> GetHiveById(int hiveId)
        => await hiveService.GetHiveAsync(hiveId);

    [HttpGet("followed")]
    public async Task<List<HiveFollowerDto>> GetFollowedHives(int? userId)
        => await hiveService.GetFollowedHivesAsync(userId ?? await User.GetUserIdAsync(context));

    [HttpPost("join")]
    public async Task JoinHive(int hiveId)
        => await hiveService.JoinHiveAsync(await User.GetUserIdAsync(context), hiveId);
    
    [HttpGet("modifyFollowRequest")]
    public async Task ApproveFollowRequest(int followRequestId, bool approve)
        => await hiveService.ModifyFollowRequestAsync(await User.GetUserIdAsync(context), followRequestId, approve);

    [HttpPost("addModerator")]
    public async Task AddModerator(int hiveId, int userId)
        => await hiveService.AddModeratorAsync(await User.GetUserIdAsync(context), hiveId, userId);

    [HttpPost("leave")]
    public async Task LeaveHive(int hiveId)
        => await hiveService.LeaveHiveAsync(await User.GetUserIdAsync(context), hiveId);

    [HttpPost("browse")]
    public async Task<PaginationResultDto<HiveDto>> BrowseHives([FromBody] HivePaginationDto pagination)
        => await hiveService.BrowseHivesAsync(pagination);

    [HttpPost("create")]
    public async Task<HiveDto> CreateHive([FromBody] CreateHiveDto hiveDto)
        => await hiveService.CreateHiveAsync(await User.GetUserIdAsync(context), hiveDto);

    [HttpPost("update")]
    public async Task<HiveDto> UpdateHive([FromBody] HiveDto hiveDto)
        => await hiveService.UpdateHiveAsync(await User.GetUserIdAsync(context), hiveDto);
}
