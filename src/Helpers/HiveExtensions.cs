using System.Linq.Expressions;

public static class HiveExtensions
{
    public static readonly Expression<Func<Hive, HiveDto>> ToDtoExpression = hive => new HiveDto
    {
        Id = hive.Id,
        Name = hive.Name,
        Description = hive.Description,
        CreatedAt = hive.CreatedAt,
        PostCount = hive.Posts.Count,
        FollowerCount = hive.Followers.Count
    };

    private static readonly Func<Hive, HiveDto> ToDtoFunc = ToDtoExpression.Compile();

    public static HiveDto ToDto(this Hive hive) => ToDtoFunc(hive);

    public static IQueryable<HiveDto> ToDto(this IQueryable<Hive> hives)
        => hives.Select(ToDtoExpression);
}