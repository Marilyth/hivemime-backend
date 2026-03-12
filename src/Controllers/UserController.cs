using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UserController(IUserService userService) : ControllerBase
{
    [HttpGet("login")]
    public LoginDto Login(string username)
    {
        return userService.Login(username);
    }

    [HttpGet]
    [Authorize]
    public UserDetailsDto GetUserDetails()
    {
        var userId = User.GetUserId();
        return userService.GetUserDetails(userId);
    }
}