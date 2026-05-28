using Microsoft.AspNetCore.Mvc;

namespace HiveMime.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HiveController(HiveService hiveService, HiveMimeContext context) : ControllerBase
{
    [HttpGet("get")]
    public async Task<HiveDto> GetHiveById(int hiveId)
        => await hiveService.GetHiveAsync(hiveId);

    [HttpGet("joined")]
    public async Task<List<HiveUserDto>> GetJoinedHives(int? userId)
        => await hiveService.GetJoinedHivesAsync(userId ?? await User.GetUserIdAsync(context));

    [HttpPost("join")]
    public async Task<HiveUserDto> JoinHive(int hiveId)
        => await hiveService.JoinHiveAsync(await User.GetUserIdAsync(context), hiveId);
    
    [HttpPost("users")]
    public async Task<PaginationResultDto<HiveUserDto>> GetUsers(int hiveId, ApprovalStatus status, [FromBody] HiveUserPaginationDto pagination)
        => await hiveService.GetUsersAsync(await User.GetUserIdAsync(context), hiveId, status, pagination);

    [HttpPatch("modifyUser")]
    public async Task ModifyHiveUser(int followRequestId, ApprovalStatus approvalStatus, MemberRole role)
        => await hiveService.ModifyHiveUserAsync(await User.GetUserIdAsync(context), followRequestId, role, approvalStatus);

    [HttpPatch("banUser")]
    public async Task BanHiveUser(int userId, int hiveId)
        => await hiveService.BanHiveUserAsync(await User.GetUserIdAsync(context), userId, hiveId);

    [HttpDelete("leave")]
    public async Task LeaveHive(int followId)
        => await hiveService.LeaveHiveAsync(await User.GetUserIdAsync(context), followId);

    [HttpPost("browse")]
    public async Task<PaginationResultDto<HiveDto>> BrowseHives([FromBody] HivePaginationDto pagination)
        => await hiveService.BrowseHivesAsync(pagination);

    [HttpPost("create")]
    public async Task<HiveDto> CreateHive([FromBody] CreateHiveDto hiveDto)
        => await hiveService.CreateHiveAsync(await User.GetUserIdAsync(context), hiveDto);

    [HttpPatch("update")]
    public async Task<HiveDto> UpdateHive([FromBody] HiveDto hiveDto)
        => await hiveService.UpdateHiveAsync(await User.GetUserIdAsync(context), hiveDto);
}
