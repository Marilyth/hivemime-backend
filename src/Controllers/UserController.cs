using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UserController(UserService userService, HiveMimeContext context) : ControllerBase
{
    [HttpGet("merge")]
    public async Task MergeAccounts(string previousJwt)
    {
        JwtSecurityTokenHandler jwtHandler = new();
        var result = jwtHandler.ValidateToken(previousJwt, Program.Parameters, out _);

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
    {
        string firebaseJson = User.FindFirst("firebase")?.Value;

        var provider = JsonDocument.Parse(firebaseJson).RootElement
            .GetProperty("sign_in_provider")
            .GetString();

        // If the user logged in with email and password, the accounts need to be verified first.
        if (provider == "password" && !User.HasClaim(c => c.Type == "email_verified" && c.Value == "true"))
            throw new Exception("User email is not verified.");

        return await userService.CreateOrLoginUserAsync(User);
    }
}