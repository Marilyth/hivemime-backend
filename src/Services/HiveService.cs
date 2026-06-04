using Mapster;
using Microsoft.EntityFrameworkCore;

public class HiveService(HiveMimeContext context, AuthorizationService authorizationService)
{
    /// <summary>
    /// Fetches and returns a hive by its ID.
    /// </summary>
    /// <param name="hiveId">The ID of the hive to fetch.</param>
    /// <returns>The hive with the specified ID.</returns>
    public async Task<HiveDto> GetHiveAsync(Guid hiveId)
        => await context.Hives.AsNoTracking()
            .QueryableFind(hiveId)
            .ProjectToType<HiveDto>()
            .FirstOrExceptionAsync();

    /// <summary>
    /// Fetches and returns all hives followed by the user.
    /// </summary>
    /// <param name="userId">The ID of the user whose followed hives to fetch.</param>
    public async Task<List<HiveUserDto>> GetJoinedHivesAsync(Guid userId)
        => await context.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .SelectMany(u => u.JoinedHives)
            .OrderByDescending(h => h.Id)
            .ProjectToType<HiveUserDto>()
            .ToListAsync();

    /// <summary>
    /// Adds a moderator to the hive, allowing them to manage the hive and its content.
    /// </summary>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <param name="hiveUserId">The ID of the user to be added as a moderator.</param>
    public async Task ModifyHiveUserAsync(Guid userId, Guid hiveUserId, MemberRole role, ApprovalStatus approvalStatus)
    {
        await authorizationService.VerifyModifyHiveUserAsync(userId, hiveUserId, role);

        HiveUser user = await context.HiveUsers.FirstOrExceptionAsync(h => h.Id == hiveUserId);

        user.Role = role;
        user.ApprovalStatus = approvalStatus;

        await context.SaveChangesAsync();
    }

    public async Task BanHiveUserAsync(Guid currentUserId, Guid userId, Guid hiveId)
    {
        await authorizationService.VerifyBanHiveUserAsync(currentUserId, userId, hiveId);

        HiveUser user = await context.HiveUsers.FirstOrDefaultAsync(h => h.UserId == userId && h.HiveId == hiveId);

        if (user is null)
        {
            user = new HiveUser
            {
                UserId = userId,
                HiveId = hiveId,
                Role = MemberRole.Guest
            };

            context.HiveUsers.Add(user);
        }

        user.ApprovalStatus = ApprovalStatus.Banned;

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Adds the user as a follower to the hive, effectively "joining" it.
    /// </summary>
    /// <param name="userId">The ID of the user joining the hive.</param>
    /// <param name="hiveId">The ID of the hive to join.</param>
    /// <returns>The DTO representing the follow relationship.</returns>
    public async Task<HiveUserDto> JoinHiveAsync(Guid userId, Guid hiveId)
    {
        await authorizationService.VerifyJoinHiveAsync(userId, hiveId);

        bool requiresApproval = await context.Hives
            .Where(h => h.Id == hiveId)
            .Select(h => h.Settings.JoinRequiresApproval)
            .FirstOrExceptionAsync();

        HiveUser hiveUser = await context.HiveUsers.FirstOrDefaultAsync(r => r.HiveId == hiveId && r.UserId == userId);
        
        if (hiveUser is null)
        {
            hiveUser = new HiveUser
            {
                HiveId = hiveId,
                UserId = userId
            };

            if (!requiresApproval)
                hiveUser.ApprovalStatus = ApprovalStatus.Approved;

            context.HiveUsers.Add(hiveUser);
        }

        hiveUser.Role = MemberRole.Follower;

        await context.SaveChangesAsync();

        return context.HiveUsers.AsNoTracking()
            .Where(r => r.Id == hiveUser.Id)
            .ProjectToType<HiveUserDto>()
            .First();
    }

    /// <summary>
    /// Fetches and returns the users of a hive, optionally filtering by pending approval status, and applying pagination.
    /// </summary>
    /// <param name="userId">The ID of the user requesting the users. Must be a moderator or the creator of the hive.</param>
    /// <param name="hiveId">The ID of the hive whose users to fetch.</param>
    /// <param name="status">Whether to fetch users with pending approval status or approved users.</param>
    /// <param name="pagination">The pagination parameters, including cursor and order by options.</param>
    /// <returns>A paginated list of users for the specified hive.</returns>
    public async Task<PaginationResultDto<HiveUserDto>> GetUsersAsync(Guid userId, Guid hiveId, ApprovalStatus status, HiveUserPaginationDto pagination)
    {
        await authorizationService.VerifyViewHiveUsersAsync(userId, hiveId);

        IQueryable<HiveUser> query = context.HiveUsers.AsNoTracking()
            .Where(r => r.HiveId == hiveId && r.ApprovalStatus == status)
            .OrderByDescending(r => r.Role);

        return await new HiveUserPaginationHelper(pagination)
            .ApplyPaginationAsync<HiveUserDto>(query);
    }

    /// <summary>
    /// Removes the user as a user from the hive, effectively "leaving" it.
    /// </summary>
    /// <param name="userId">The ID of the user leaving the hive.</param>
    /// <param name="hiveUserId">The ID of the user relationship to remove.</param>
    public async Task LeaveHiveAsync(Guid userId, Guid hiveUserId)
    {
        await authorizationService.VerifyLeaveHiveAsync(userId, hiveUserId);
        HiveUser hiveUser = await context.HiveUsers.FirstOrExceptionAsync(f => f.Id == hiveUserId);

        context.HiveUsers.Remove(hiveUser);

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Fetches and returns hives depending on the provided filter and pagination parameters.
    /// </summary>
    /// <param name="pagination">The pagination parameters, including filter and order by options.</param>
    public async Task<PaginationResultDto<HiveDto>> BrowseHivesAsync(HivePaginationDto pagination)
    {
        IQueryable<Hive> query = context.Hives
            .Where(h => !h.Settings.IsPrivate)
            .AsNoTracking();

        return await new HivePaginationHelper(pagination)
            .ApplyPaginationAsync<HiveDto>(query);
    }

    /// <summary>
    /// Creates and returns a new hive based on the provided data.
    /// </summary>
    /// <param name="userId">The ID of the user creating the hive.</param>
    /// <param name="hiveDto">The data for the new hive.</param>
    /// <returns>The created hive user relationship.</returns>
    public async Task<HiveUserDto> CreateHiveAsync(Guid userId, CreateHiveDto hiveDto)
    {
        await authorizationService.VerifyCreateHiveAsync(userId);

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
            Posts = [],
            Users = [new() { UserId = userId, ApprovalStatus = ApprovalStatus.Approved, Role = MemberRole.Creator }],
            Settings = new()
        };

        context.Hives.Add(hive);
        await context.SaveChangesAsync();

        return await context.HiveUsers.AsNoTracking()
            .Where(r => r.UserId == userId && r.HiveId == hive.Id)
            .ProjectToType<HiveUserDto>()
            .FirstOrExceptionAsync();
    }

    public async Task<HiveDto> UpdateHiveAsync(Guid userId, HiveDto hiveDto)
    {
        Hive hive = await context.Hives
            .Include(h => h.Settings)
            .FirstOrExceptionAsync(h => h.Id == hiveDto.Id);

        if (hive == null)
            throw new ValidationException("Hive not found.");

        await authorizationService.VerifyEditHiveAsync(userId, hive.Id);

        hive.Name = hiveDto.Name?.Trim();
        hive.Description = hiveDto.Description?.Trim();

        hive.Settings.IsPrivate = hiveDto.Settings.IsPrivate;
        hive.Settings.JoinRequiresApproval = hiveDto.Settings.JoinRequiresApproval;

        hive.Settings.PostRequiresApproval = hiveDto.Settings.PostRequiresApproval;
        hive.Settings.MinHoneyToPost = hiveDto.Settings.MinHoneyToPost;
        hive.Settings.MinRoleToPost = hiveDto.Settings.MinRoleToPost;

        hive.Settings.MinHoneyToComment = hiveDto.Settings.MinHoneyToComment;
        hive.Settings.MinRoleToComment = hiveDto.Settings.MinRoleToComment;

        await context.SaveChangesAsync();

        return hive.ToQueryable(context).ProjectToType<HiveDto>().First();
    }
}