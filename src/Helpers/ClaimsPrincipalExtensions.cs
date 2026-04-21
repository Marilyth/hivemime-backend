using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

public static class ClaimsPrincipalExtensions
{
    public static async Task<int> GetUserIdAsync(this ClaimsPrincipal user, HiveMimeContext context)
    {
        Claim userIdClaim = user.FindFirst("user_id")!;

        if (userIdClaim is null)
            return -1;

        return await context.Users.Where(u => u.FirebaseId == userIdClaim.Value)
            .Select(u => u.Id).FirstOrDefaultAsync();
    }
}
