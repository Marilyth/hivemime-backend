using System.Security.Claims;

public static class ClaimsPrincipalExtensions
{
    public static User GetUser(this ClaimsPrincipal user, HiveMimeContext context)
    {
        int userId = user.GetUserId();

        return userId == -1 ? null : context.Users.Find(userId);
    }

    public static int GetUserId(this ClaimsPrincipal user)
    {
        Claim userIdClaim = user.FindFirst("UserId")!;

        return userIdClaim is null ? -1 : int.Parse(userIdClaim.Value);
    }
}
