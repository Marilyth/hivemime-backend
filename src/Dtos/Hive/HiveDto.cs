public class HiveUserDto : IHasIdentifier
{
    public int Id { get; set; }
    public HiveDto Hive { get; set; }
    public UserDto User { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class HiveDto : IHasIdentifier
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int PostCount { get; set; }
    public int UserCount { get; set; }
    public HiveOptionsDto Settings { get; set; }
}

public class HiveOptionsDto
{
    public bool MustBeApprovedToPost { get; set; }
    public bool MustBeApprovedToJoin { get; set; }

    public double MinHoneyToPost { get; set; }
    public PostPolicy PostPolicy { get; set; }
}