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
            .ToDto()
            .FirstOrExceptionAsync(h => h.Id == hiveId);

    /// <summary>
    /// Fetches and returns all hives followed by the user.
    /// </summary>
    /// <param name="userId">The ID of the user whose followed hives to fetch.</param>
    public async Task<List<HiveDto>> GetFollowedHivesAsync(int userId)
        => await context.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .SelectMany(u => u.FollowedHives)
            .OrderByDescending(h => h.Id)
            .ToDto()
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
    /// <param name="filter">The filter to apply to the hives, based on their name.</param>
    public async Task<List<HiveDto>> BrowseHivesAsync(int? afterId, string filter)
    {
        IQueryable<Hive> query = context.Hives.AsNoTracking();

        if (afterId.HasValue)
            query = query.Where(h => h.Id < afterId.Value);

        if (!string.IsNullOrWhiteSpace(filter))
            query = query.Where(h => h.Name.ToLower().Contains(filter.Trim().ToLower()));

        return await query.OrderBy(h => h.Id)
            .ToDto()
            .ToListAsync();
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
            throw new InvalidOperationException("Hive names must be at least 3 characters long.");

        if (await context.Hives.AnyAsync(h => h.Name.ToLower() == name.ToLower()))
            throw new InvalidOperationException("A hive with the same name already exists.");

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

        return hive.ToDto();
    }
}