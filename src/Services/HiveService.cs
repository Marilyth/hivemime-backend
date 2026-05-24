using Mapster;
using Microsoft.EntityFrameworkCore;

public class HiveService(HiveMimeContext context, AuthorizationService authorizationService)
{
    /// <summary>
    /// Fetches and returns a hive by its ID.
    /// </summary>
    /// <param name="hiveId">The ID of the hive to fetch.</param>
    /// <returns>The hive with the specified ID.</returns>
    public async Task<HiveDto> GetHiveAsync(int hiveId)
        => await context.Hives.AsNoTracking()
            .QueryableFind(hiveId)
            .ProjectToType<HiveDto>()
            .FirstAsync();

    /// <summary>
    /// Fetches and returns all hives followed by the user.
    /// </summary>
    /// <param name="userId">The ID of the user whose followed hives to fetch.</param>
    public async Task<List<HiveFollowerDto>> GetFollowedHivesAsync(int userId)
        => await context.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .SelectMany(u => u.FollowedHives)
            .OrderByDescending(h => h.Id)
            .ProjectToType<HiveFollowerDto>()
            .ToListAsync();

    /// <summary>
    /// Adds a moderator to the hive, allowing them to manage the hive and its content.
    /// </summary>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <param name="moderatorId">The ID of the user to be added as a moderator.</param>
    /// <param name="hiveId">The ID of the hive to which the moderator will be added.</param>
    public async Task AddModeratorAsync(int userId, int moderatorId, int hiveId)
    {
        User user = new() { Id = userId };
        Hive hive = new()
        {
            Id = hiveId,
            Moderators = []
        };

        context.Hives.Attach(hive);
        context.Users.Attach(user);

        hive.Moderators.Add(user);

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Adds the user as a follower to the hive, effectively "joining" it.
    /// <param name="userId">The ID of the user joining the hive.</param>
    /// <param name="hiveId">The ID of the hive to join.</param>
    /// <returns></returns>
    public async Task JoinHiveAsync(int userId, int hiveId)
    {
        bool requiresApproval = await context.Hives
            .Where(h => h.Id == hiveId)
            .Select(h => h.Settings.MustBeApprovedToJoin)
            .FirstOrExceptionAsync();

        if (await context.HiveFollowers.AnyAsync(r => r.HiveId == hiveId && r.UserId == userId))
                throw new ValidationException("You have already requested to join this hive.");
        
        HiveFollower joinRequest = new()
        {
            HiveId = hiveId,
            UserId = userId
        };

        context.HiveFollowers.Add(joinRequest);

        if (!requiresApproval)
            joinRequest.IsApproved = true;

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Approves a user's request to follow a hive, adding them as a follower and removing the join request.
    /// </summary>
    /// <param name="userId">The ID of the user approving the follow request.</param>
    /// <param name="followRequestId">The ID of the follow request to approve.</param>
    public async Task ModifyFollowRequestAsync(int userId, int followRequestId, bool approve)
    {   
        HiveFollower request = await context.HiveFollowers.FirstOrExceptionAsync(r => r.Id == followRequestId);
        await authorizationService.VerifyApproveFollowRequestAsync(userId, followRequestId);

        if (!approve)
            context.HiveFollowers.Remove(request);
        else
            request.IsApproved = true;

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Removes the user as a follower from the hive, effectively "leaving" it.
    /// </summary>
    /// <param name="userId">The ID of the user leaving the hive.</param>
    /// <param name="hiveId">The ID of the hive to leave.</param>
    public async Task LeaveHiveAsync(int userId, int hiveId)
    {
        await context.HiveFollowers.Where(f => f.HiveId == hiveId && f.UserId == userId).ExecuteDeleteAsync();
    }

    /// <summary>
    /// Fetches and returns hives depending on the provided filter and pagination parameters.
    /// </summary>
    /// <param name="pagination">The pagination parameters, including filter and order by options.</param>
    public async Task<PaginationResultDto<HiveDto>> BrowseHivesAsync(HivePaginationDto pagination)
    {
        IQueryable<Hive> query = context.Hives.AsNoTracking();

        return await query.ApplyPaginationFilter(pagination)
            .ApplyPaginationOrdering(pagination)
            .ApplyPaginationPageSize(pagination)
            .FetchPaginationResultAsync(pagination);
    }

    /// <summary>
    /// Creates and returns a new hive based on the provided data.
    /// </summary>
    /// <param name="userId">The ID of the user creating the hive.</param>
    /// <param name="hiveDto">The data for the new hive.</param>
    /// <returns>The created hive.</returns>
    public async Task<HiveDto> CreateHiveAsync(int userId, CreateHiveDto hiveDto)
    {
        string name = hiveDto.Name?.Trim();
        string description = hiveDto.Description?.Trim();

        if (string.IsNullOrWhiteSpace(name) || name.Length < 3)
            throw new ValidationException("Hive names must be at least 3 characters long.");

        if (await context.Hives.AnyAsync(h => h.Name.ToLower() == name.ToLower()))
            throw new ValidationException("A hive with the same name already exists.");

        Hive hive = new()
        {
            Name = name,
            Description = description,
            CreatorId = userId,
            Posts = [],
            Followers = [new() { UserId = userId, IsApproved = true }]
        };

        context.Hives.Add(hive);
        await context.SaveChangesAsync();

        return hive.ToQueryable(context).ProjectToType<HiveDto>().First();
    }

    public async Task<HiveDto> UpdateHiveAsync(int userId, HiveDto hiveDto)
    {
        Hive hive = await context.Hives
            .Include(h => h.Moderators)
            .Include(h => h.Settings)
            .FirstOrExceptionAsync(h => h.Id == hiveDto.Id);

        if (hive == null)
            throw new ValidationException("Hive not found.");

        await authorizationService.VerifyEditHiveAsync(userId, hive.Id);

        hive.Name = hiveDto.Name?.Trim();
        hive.Description = hiveDto.Description?.Trim();
        hive.Settings.MinHoneyToPost = hiveDto.Settings.MinHoneyToPost;
        hive.Settings.MustBeApprovedToJoin = hiveDto.Settings.MustBeApprovedToJoin;
        hive.Settings.MustBeApprovedToPost = hiveDto.Settings.MustBeApprovedToPost;
        hive.Settings.PostPolicy = hiveDto.Settings.PostPolicy;

        context.Hives.Update(hive);
        await context.SaveChangesAsync();

        return hive.ToQueryable(context).ProjectToType<HiveDto>().First();
    }
}