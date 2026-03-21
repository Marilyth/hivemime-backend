using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UserController(UserService userService) : ControllerBase
{
    [HttpGet("login")]
    public async Task<LoginDto> Login(string username)
        => await userService.LoginAsync(username);

    [HttpGet("details")]
    [Authorize]
    public async Task<UserDetailsDto> GetUserDetails()
        => await userService.GetUserDetailsAsync(User.GetUserId());
}