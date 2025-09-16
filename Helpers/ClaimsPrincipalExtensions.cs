using System.Security.Claims;

public static class ClaimsPrincipalExtensions
{
    public static User? GetUser(this ClaimsPrincipal user, HiveMimeContext context)
    {
        var userIdClaim = user.FindFirst("UserId");

        return context.Users.Find(int.Parse(userIdClaim?.Value));
    }
}
