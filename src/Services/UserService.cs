using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Caching.Hybrid;

public class UserService(HiveMimeContext context, IConfiguration configuration, GeoIPService geoIPService, HybridCache cache)
{
    private HashSet<string> _countries = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
        .Select(c => new RegionInfo(c.Name).TwoLetterISORegionName)
        .ToHashSet();

    /// <summary>
    /// Returns the details of a user, including their settings.
    /// </summary>
    /// <param name="userId">The ID of the user to retrieve details for.</param>
    public async Task<UserDetailsDto> GetUserDetailsAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new NotFoundException("User does not exist.");

        return await cache.GetOrCreateAsync(CacheHelper.GetCacheKey([userId]), async entry =>
        {
            return await context.Users.AsNoTracking()
                .QueryableFind(userId)
                .ProjectToType<UserDetailsDto>()
                .FirstOrExceptionAsync();
        });
    }

    /// <summary>
    /// Returns a paginated list of users based on the provided pagination parameters, including filtering and sorting options.
    /// </summary>
    /// <param name="pagination">The pagination parameters, including filtering and sorting options.</param>
    /// <returns>A paginated list of user profiles.</returns>
    public async Task<PaginationResultDto<UserDto>> BrowseUsersAsync(UserPaginationDto pagination)
    {
        return await cache.GetOrCreateAsync(CacheHelper.GetCacheKey([pagination]), async entry =>
        {
            return await new UserPaginationHelper(pagination)
                .ApplyPaginationAsync<UserDto>(context.Users.AsNoTracking());
        });
    }

    /// <summary>
    /// Returns the basic public profile of a user, including their honey and post/comment counts.
    /// </summary>
    /// <param name="userId">The ID of the user to retrieve the profile for.</param>
    /// <returns>The user's profile information.</returns>
    /// <exception cref="NotFoundException">Thrown when the user does not exist.</exception>
    public async Task<UserProfileDto> GetUserProfileAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new NotFoundException("User does not exist.");

        return await cache.GetOrCreateAsync(CacheHelper.GetCacheKey([userId]), async entry =>
        {
            return await context.Users.AsNoTracking()
                .QueryableFind(userId)
                .ProjectToType<UserProfileDto>()
                .FirstOrExceptionAsync();
        });
    }

    /// <summary>
    /// Creates a new user for the given claim or returns the existing user if a user with the same UID already exists.
    /// </summary>
    /// <param name="user">The claims principal containing the user information.</param>
    public async Task<UserDetailsDto> CreateOrLoginUserAsync(ClaimsPrincipal user)
    {
        string uid = user.FindFirstValue("user_id")
            ?? throw new ValidationException("User ID claim is missing.");

        if (await context.Users.FirstOrDefaultAsync(u => u.FirebaseId == uid) is not User existingUser)
        {
            var ipAddress = context.GetService<IHttpContextAccessor>()?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
            string? country = await geoIPService.GetCountryOfIPAsync(ipAddress);

            existingUser = new User()
            {
                FirebaseId = uid,
                IsAnonymous = true,
                Settings = new()
                {
                    Country = country
                }
            };

            await SetUsernameAsync(existingUser, "guest_" + Guid.NewGuid().ToString(), true);

            context.Users.Add(existingUser);
        }

        await UpdateUserStatus(user, existingUser);
        return await GetUserDetailsAsync(existingUser.Id);
    }

    /// <summary>
    /// Updates the user's details, including their username and settings. Validates the input and checks for username uniqueness.
    /// </summary>
    /// <param name="userId">The ID of the user to update.</param>
    /// <param name="userDetails">The new details for the user.</param>
    /// <returns>The updated user details.</returns>
    /// <exception cref="ValidationException">Thrown when the input is invalid.</exception>
    public async Task<UserDetailsDto> UpdateUserAsync(Guid userId, UserDetailsDto userDetails)
    {
        var user = await context.Users.Include(u => u.Settings).FirstOrExceptionAsync(u => u.Id == userId);
        await SetUsernameAsync(user, userDetails.Username, false);

        if (!string.IsNullOrEmpty(userDetails.Settings.Country) && !_countries.Contains(userDetails.Settings.Country))
            throw new ValidationException("Invalid country code.");

        if (userDetails.DateOfBirth.HasValue)
        {
            DateTimeOffset minAge = DateTimeOffset.UtcNow.AddYears(-13);
            if (userDetails.DateOfBirth.Value > minAge)
                throw new ValidationException("You must be at least 13 years old to use this service.");

            DateTimeOffset maxAge = DateTimeOffset.UtcNow.AddYears(-130);
            if (userDetails.DateOfBirth.Value < maxAge)
                throw new ValidationException("Invalid date of birth.");

            user.DateOfBirth = userDetails.DateOfBirth;
        }

        user.Settings.Country = userDetails.Settings.Country;
        user.Settings.ShareAgeOnVote = userDetails.Settings.ShareAgeOnVote;
        user.Settings.ShareCountryOnVote = userDetails.Settings.ShareCountryOnVote;
        user.Settings.ShareDateOnVote = userDetails.Settings.ShareDateOnVote;
        user.Settings.ProtectVoteOnFilter = userDetails.Settings.ProtectVoteOnFilter;

        await context.SaveChangesAsync();
        return await GetUserDetailsAsync(user.Id);
    }

    /// <summary>
    /// Merges a previous user account into the current one.
    /// This is used to merge an anonymous account with a new account after login,
    /// so that the user's posts, comments, votes, etc. are not lost.
    /// </summary>
    /// <param name="currentUserId">The ID of the current user.</param>
    /// <param name="previousUserId">The ID of the previous user to merge.</param>
    public async Task MergeAccountsAsync(Guid currentUserId, Guid previousUserId)
    {
        var currentUser = await context.Users.Include(u => u.JoinedHives)
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

        // Merge followed hives.
        var currentFollowedHives = context.HiveUsers.Where(u => u.UserId == currentUserId)
            .Select(h => h.HiveId);

        var previousFollowedHives = await context.HiveUsers.Where(u => u.UserId == previousUserId)
            .Where(h => !currentFollowedHives.Contains(h.HiveId))
            .ToListAsync();

        foreach (var hive in previousFollowedHives)
            hive.UserId = currentUserId;

        // Merge honey.
        currentUser.Honey += previousUser.Honey;
        
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
            string newUsername = "";

            if (claims.HasClaim(c => c.Type == "name"))
                newUsername = claims.FindFirst("name")!.Value;
            else if (email is not null)
                newUsername = email.Split('@')[0];
            else
                newUsername = "user_" + Guid.NewGuid().ToString();

            newUsername = newUsername.Length > 64 ? newUsername[..64] : newUsername;

            await SetUsernameAsync(user, newUsername, true);
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

    private async Task SetUsernameAsync(User user, string newUserName, bool regenerateIfCollision)
    {
        newUserName = newUserName.Trim();
        
        if (newUserName.Length < 3)
            throw new ValidationException("Username must be at least 3 characters long.");

        if (newUserName.Length > 64)
            throw new ValidationException("Username must be at most 64 characters long.");

        if (user.Username != newUserName)
        {
            if (await context.Users.AnyAsync(u => u.Username == newUserName && u.Id != user.Id))
            {
                if (!regenerateIfCollision)
                    throw new ValidationException($"Username \"{newUserName}\" is already taken. Please pick another one.");

                string baseUserName = newUserName.Length > 55 ? newUserName[..55] : newUserName;
                newUserName = baseUserName + "_" + Guid.NewGuid().ToString()[..8];

                await SetUsernameAsync(user, newUserName, false);
                return;
            }

            user.Username = newUserName;
        }
    }
}