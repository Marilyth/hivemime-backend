using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IUserService userService, HiveMimeContext context) : ControllerBase
{
    [HttpGet("login")]
    public LoginDto Login(string username)
    {
        return userService.Login(username);
    }
}