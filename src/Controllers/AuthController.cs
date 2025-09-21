using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IUserService userService, HiveMimeContext context) : ControllerBase
{
    [HttpGet("login")]
    public LoginDto Login()
    {
        return userService.Login(User.GetUser(context));
    }
}