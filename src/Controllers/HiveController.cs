using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HiveMime.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HiveController(HiveService hiveService, HiveMimeContext context) : ControllerBase
{
    [HttpGet("get")]
    [EnableRateLimiting("1/1s")]
    public async Task<HiveDto> GetHiveById(Guid hiveId)
        => await hiveService.GetHiveAsync(hiveId);

    [HttpGet("joined")]
    [EnableRateLimiting("1/1s")]
    public async Task<List<HiveUserDto>> GetJoinedHives(Guid? userId)
        => await hiveService.GetJoinedHivesAsync(userId ?? await User.GetUserIdAsync(context));

    [HttpPost("join")]
    [EnableRateLimiting("1/1s")]
    public async Task<HiveUserDto> JoinHive(Guid hiveId)
        => await hiveService.JoinHiveAsync(await User.GetUserIdAsync(context), hiveId);
    
    [HttpPost("users")]
    [EnableRateLimiting("1/1s")]
    public async Task<PaginationResultDto<HiveUserDto>> GetUsers(Guid hiveId, ApprovalStatus status, [FromBody] HiveUserPaginationDto pagination)
        => await hiveService.GetUsersAsync(await User.GetUserIdAsync(context), hiveId, status, pagination);

    [HttpPatch("modifyUser")]
    [EnableRateLimiting("1/1s")]
    public async Task ModifyHiveUser(Guid followRequestId, ApprovalStatus approvalStatus, MemberRole role)
        => await hiveService.ModifyHiveUserAsync(await User.GetUserIdAsync(context), followRequestId, role, approvalStatus);

    [HttpPatch("banUser")]
    [EnableRateLimiting("1/1s")]
    public async Task BanHiveUser(Guid userId, Guid hiveId)
        => await hiveService.BanHiveUserAsync(await User.GetUserIdAsync(context), userId, hiveId);

    [HttpDelete("leave")]
    [EnableRateLimiting("5/5s")]
    public async Task LeaveHive(Guid followId)
        => await hiveService.LeaveHiveAsync(await User.GetUserIdAsync(context), followId);

    [HttpPost("browse")]
    [EnableRateLimiting("5/1s")]
    public async Task<PaginationResultDto<HiveDto>> BrowseHives([FromBody] HivePaginationDto pagination)
        => await hiveService.BrowseHivesAsync(pagination);

    [HttpPost("create")]
    [EnableRateLimiting("1/1m")]
    public async Task<HiveUserDto> CreateHive([FromBody] CreateHiveDto hiveDto)
        => await hiveService.CreateHiveAsync(await User.GetUserIdAsync(context), hiveDto);

    [HttpPatch("update")]
    [EnableRateLimiting("1/5s")]
    public async Task<HiveDto> UpdateHive([FromBody] HiveDto hiveDto)
        => await hiveService.UpdateHiveAsync(await User.GetUserIdAsync(context), hiveDto);
}
