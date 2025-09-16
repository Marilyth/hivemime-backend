using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UserController(HiveMimeContext context) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public User GetUserDetails()
    {
        var user = User.GetUser(context);

        return user;
    }
}