using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

public class HiveService(HiveMimeContext context) : IHiveService
{
    public HiveDto GetHiveById(int hiveId)
        => context.Hives.AsNoTracking()
            .ToDto()
            .FirstOrException(h => h.Id == hiveId);

    public List<HiveDto> BrowseHives(int? afterId, string filter)
    {
        IQueryable<Hive> query = context.Hives.AsNoTracking();

        if (afterId.HasValue)
            query = query.Where(h => h.Id < afterId.Value);

        if (!string.IsNullOrWhiteSpace(filter))
            query = query.Where(h => h.Name.ToLower().Contains(filter.Trim().ToLower()));

        return query.OrderBy(h => h.Id)
            .ToDto()
            .ToList();
    }

    public HiveDto CreateHive(int userId, CreateHiveDto hiveDto)
    {
        string name = hiveDto.Name?.Trim();
        string description = hiveDto.Description?.Trim();

        if (string.IsNullOrWhiteSpace(name) || name.Length < 3)
            throw new InvalidOperationException("Hive names must be at least 3 characters long.");

        if (context.Hives.Any(h => h.Name.ToLower() == name.ToLower()))
            throw new InvalidOperationException("A hive with the same name already exists.");

        Hive hive = new()
        {
            Name = name,
            Description = description,
            CreatorId = userId,
            Posts = [],
            Followers = [context.Users.Find(userId)]
        };

        context.Hives.Add(hive);
        context.SaveChanges();

        return hive.ToDto();
    }
}