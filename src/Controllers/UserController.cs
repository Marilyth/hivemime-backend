using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

[ApiController]
[Route("api/[controller]")]
public class UserController(UserService userService, HiveMimeContext context, IOptionsMonitor<JwtBearerOptions> optionsMonitor) : ControllerBase
{
    [HttpGet("merge")]
    public async Task MergeAccounts(string previousJwt)
    {
        var jwtOptions = optionsMonitor.Get(JwtBearerDefaults.AuthenticationScheme);

        jwtOptions.TokenValidationParameters.ConfigurationManager =
            (BaseConfigurationManager)jwtOptions.ConfigurationManager;
            
        JwtSecurityTokenHandler jwtHandler = new();
        var result = jwtHandler.ValidateToken(previousJwt, jwtOptions.TokenValidationParameters, out _);

        // Merges must be on an anonymous account through the API.
        if (result.FindFirst("provider_id")!.Value != "anonymous")
            throw new Exception("Previous token is not valid.");

        await userService.MergeAccountsAsync(
            await User.GetUserIdAsync(context),
            await result.GetUserIdAsync(context));
    }

    [HttpGet("me")]
    public async Task<UserDetailsDto> GetUserDetails()
        => await userService.GetUserDetailsAsync(await User.GetUserIdAsync(context));

    [HttpGet("login")]
    public async Task<UserDetailsDto> LoginUser()
        => await userService.CreateOrLoginUserAsync(User);
}