using Mapster;
using Microsoft.EntityFrameworkCore;

public class HiveService(HiveMimeContext context)
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
    public async Task<List<HiveDto>> GetFollowedHivesAsync(int userId)
        => await context.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .SelectMany(u => u.FollowedHives)
            .OrderByDescending(h => h.Id)
            .ProjectToType<HiveDto>()
            .ToListAsync();

    /// <summary>
    /// Adds the user as a follower to the hive, effectively "joining" it.
    /// <param name="userId">The ID of the user joining the hive.</param>
    /// <param name="hiveId">The ID of the hive to join.</param>
    /// <returns></returns>
    public async Task JoinHiveAsync(int userId, int hiveId)
    {
        User user = new() { Id = userId };
        Hive hive = new()
        {
            Id = hiveId,
            Followers = []
        };

        context.Hives.Attach(hive);
        context.Users.Attach(user);

        hive.Followers.Add(user);

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Removes the user as a follower from the hive, effectively "leaving" it.
    /// </summary>
    /// <param name="userId">The ID of the user leaving the hive.</param>
    /// <param name="hiveId">The ID of the hive to leave.</param>
    public async Task LeaveHiveAsync(int userId, int hiveId)
    {
        User user = new() { Id = userId };
        Hive hive = new()
        {
            Id = hiveId,
            Followers = [user]
        };

        context.Hives.Attach(hive);
        context.Users.Attach(user);

        hive.Followers.Remove(user);

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Fetches and returns hives depending on the provided filter and pagination parameters.
    /// </summary>
    /// <param name="afterId">The ID of the last hive seen, for pagination.</param>
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
            Followers = [await context.Users.FindAsync(userId)]
        };

        context.Hives.Add(hive);
        await context.SaveChangesAsync();

        return hive.ToQueryable(context).ProjectToType<HiveDto>().First();
    }
}