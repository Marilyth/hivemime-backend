using System.Security.Claims;
using System.Text.Json;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

public class UserService(HiveMimeContext context, IConfiguration configuration, GeoIPService geoIPService)
{
    /// <summary>
    /// Returns the details of a user, including their settings.
    /// </summary>
    /// <param name="userId">The ID of the user to retrieve details for.</param>
    public async Task<UserDetailsDto> GetUserDetailsAsync(int userId)
    {
        if (userId == 0)
            throw new Exception("User does not exist.");

        var user = await context.Users.AsNoTracking().Where(u => u.Id == userId).Include(u => u.Settings).FirstAsync();
        return user.Adapt<UserDetailsDto>();
    }

    /// <summary>
    /// Creates a new user for the given claim or returns the existing user if a user with the same UID already exists.
    /// </summary>
    /// <param name="user">The claims principal containing the user information.</param>
    public async Task<UserDetailsDto> CreateOrLoginUserAsync(ClaimsPrincipal user)
    {
        string uid = user.FindFirstValue("user_id")
            ?? throw new Exception("User ID claim is missing.");

        if (await context.Users.FirstOrDefaultAsync(u => u.FirebaseId == uid) is not User existingUser)
        {
            var ipAddress = context.GetService<IHttpContextAccessor>()?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
            string? country = await geoIPService.GetCountryOfIPAsync(ipAddress);

            existingUser = new User()
            {
                Username = "guest_" + Guid.NewGuid().ToString(),
                FirebaseId = uid,
                IsAnonymous = true,
                Settings = new()
                {
                    Country = country
                }
            };

            context.Users.Add(existingUser);
        }

        await UpdateUserStatus(user, existingUser);
        return await GetUserDetailsAsync(existingUser.Id);
    }

    /// <summary>
    /// Merges a previous user account into the current one.
    /// This is used to merge an anonymous account with a new account after login,
    /// so that the user's posts, comments, votes, etc. are not lost.
    /// </summary>
    /// <param name="currentUserId">The ID of the current user.</param>
    /// <param name="previousUserId">The ID of the previous user to merge.</param>
    public async Task MergeAccountsAsync(int currentUserId, int previousUserId)
    {
        var currentUser = await context.Users.Include(u => u.FollowedHives)
            .FirstOrExceptionAsync(u => u.Id == currentUserId);
        var previousUser = await context.Users.FirstOrExceptionAsync(u => u.Id == previousUserId);

        using var transaction = await context.Database.BeginTransactionAsync();
        
        // Merge posts.
        await context.Posts.Where(p => p.CreatorId == previousUserId)
            .ExecuteUpdateAsync(p => p.SetProperty(post => post.CreatorId, currentUserId));

        // Merge comments.
        await context.Comments.Where(c => c.UserId == previousUserId)
            .ExecuteUpdateAsync(c => c.SetProperty(comment => comment.UserId, currentUserId));

        // Merge votes. Discard previous votes already present.
        var currentVotes = context.PostVotes.Where(v => v.UserId == currentUserId)
            .Select(v => v.PostId);

        await context.PostVotes.Where(v => v.UserId == previousUserId)
            .Where(v => !currentVotes.Contains(v.PostId))
            .ExecuteUpdateAsync(v => v.SetProperty(vote => vote.UserId, currentUserId));

        // Merge hives.
        await context.Hives.Where(h => h.CreatorId == previousUserId)
            .ExecuteUpdateAsync(h => h.SetProperty(hive => hive.CreatorId, currentUserId));

        // Merge followed hives.
        var currentFollowedHives = context.Users.Where(u => u.Id == currentUserId)
            .SelectMany(u => u.FollowedHives)
            .Select(h => h.Id);

        var previousFollowedHives = context.Users.Where(u => u.Id == previousUserId)
            .SelectMany(u => u.FollowedHives)
            .Where(h => !currentFollowedHives.Contains(h.Id))
            .ToListAsync();

        foreach (var hive in await previousFollowedHives)
            currentUser.FollowedHives.Add(hive);
        
        // Remove previous user.
        context.Users.Remove(previousUser);

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    /// <summary>
    /// Updates the user's status based on their claims.
    /// </summary>
    /// <param name="claims">The claims principal containing the user information.</param>
    /// <param name="user">The user to update.</param>
    private async Task UpdateUserStatus(ClaimsPrincipal claims, User user)
    {
        string firebaseJson = claims.FindFirst("firebase").Value;

        var firebaseDocument = JsonDocument.Parse(firebaseJson).RootElement;
        var provider = firebaseDocument.GetProperty("sign_in_provider").GetString();
        var identities = firebaseDocument.GetProperty("identities")
            .EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.EnumerateArray().Select(v => v.GetString()).ToList());

        var email = identities.ContainsKey("email") ? identities["email"].First() :
            claims.HasClaim(c => c.Type == "email") ? claims.FindFirst("email").Value : null;

        // If the user has a guest name, overwrite it with a new one.
        if (user.IsAnonymous && provider != "anonymous")
        {
            if (claims.HasClaim(c => c.Type == "name"))
                user.Username = claims.FindFirst("name")!.Value;
            else if (email is not null)
                user.Username = email.Split('@')[0];
            else
                user.Username = "user_" + Guid.NewGuid().ToString();

            user.Username = user.Username.Length > 64 ? user.Username[..64] : user.Username;

            // This is not perfect but if it collides just try again.
            if (context.Users.Any(u => u.Username == user.Username && u.Id != user.Id))
            {
                user.Username = user.Username.Length > 55 ? user.Username[..55] : user.Username;
                user.Username += "_" + Guid.NewGuid().ToString()[..8];
            }

            user.IsAnonymous = false;
        }

        if (provider == "anonymous" ||
            claims.HasClaim(c => c.Type == "email_verified" && c.Value == "false"))
            user.IsVerified = false;
        else
            user.IsVerified = true;

        user.LastLogin = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();
    }
}