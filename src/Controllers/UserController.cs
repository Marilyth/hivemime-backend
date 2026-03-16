using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UserController(IUserService userService) : ControllerBase
{
    [HttpGet("login")]
    public LoginDto Login(string username)
        => userService.Login(username);

    [HttpGet]
    [Authorize]
    public UserDetailsDto GetUserDetails()
        => userService.GetUserDetails(User.GetUserId());
}