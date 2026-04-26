public class HiveDto : IHasIdentifier
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int PostCount { get; set; }
    public int FollowerCount { get; set; }
}