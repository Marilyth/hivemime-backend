using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UserController(UserService userService) : ControllerBase
{
    [HttpGet("login")]
    [AllowAnonymous]
    public async Task<LoginDto> Login(string username)
        => await userService.LoginAsync(username);

    [HttpGet("details")]
    public async Task<UserDetailsDto> GetUserDetails()
        => await userService.GetUserDetailsAsync(User.GetUserId());
}