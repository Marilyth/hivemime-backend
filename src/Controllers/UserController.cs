using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

[ApiController]
[Route("api/[controller]")]
public class UserController(UserService userService, HiveMimeContext context, IOptionsMonitor<JwtBearerOptions> optionsMonitor) : ControllerBase
{
    [HttpGet("merge")]
    [EnableRateLimiting("1/1m")]
    public async Task MergeAccounts(string previousJwt)
    {
        var jwtOptions = optionsMonitor.Get(JwtBearerDefaults.AuthenticationScheme);

        jwtOptions.TokenValidationParameters.ConfigurationManager =
            (BaseConfigurationManager)jwtOptions.ConfigurationManager;
            
        JwtSecurityTokenHandler jwtHandler = new();
        var result = jwtHandler.ValidateToken(previousJwt, jwtOptions.TokenValidationParameters, out _);

        // Merges must be on an anonymous account through the API.
        if (result.FindFirst("provider_id")!.Value != "anonymous")
            throw new ValidationException("Previous token is not valid.");

        await userService.MergeAccountsAsync(
            await User.GetUserIdAsync(context),
            await result.GetUserIdAsync(context));
    }

    [HttpGet("me")]
    [EnableRateLimiting("1/1s")]
    public async Task<UserDetailsDto> GetUserDetails()
        => await userService.GetUserDetailsAsync(await User.GetUserIdAsync(context));

    [HttpPost("browse")]
    [EnableRateLimiting("5/1s")]
    public async Task<PaginationResultDto<UserDto>> BrowseUsers([FromBody] UserPaginationDto pagination)
        => await userService.BrowseUsersAsync(pagination);

    [HttpGet("profile")]
    [EnableRateLimiting("1/1s")]
    public async Task<UserProfileDto> GetUserProfile(Guid userId)
        => await userService.GetUserProfileAsync(userId);

    [HttpGet("login")]
    [EnableRateLimiting("1/1s")]
    public async Task<UserDetailsDto> LoginUser()
        => await userService.CreateOrLoginUserAsync(User);

    [HttpPost("update")]
    [EnableRateLimiting("1/5s")]
    public async Task<UserDetailsDto> UpdateUser([FromBody] UserDetailsDto userDetails)
        => await userService.UpdateUserAsync(await User.GetUserIdAsync(context), userDetails);
}