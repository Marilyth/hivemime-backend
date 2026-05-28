public class HiveUserDto : IHasIdentifier
{
    public int Id { get; set; }
    public HiveDto Hive { get; set; }
    public UserDto User { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; }
    public MemberRole Role { get; set; }
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
    public HiveSettingsDto Settings { get; set; }
}

public class HiveSettingsDto
{
    public bool IsPrivate { get; set; }

    public bool JoinRequiresApproval { get; set; }
    public double MinHoneyToJoin { get; set; }

    public bool PostRequiresApproval { get; set; }
    public double MinHoneyToPost { get; set; }
    public MemberRole MinRoleToPost { get; set; }

    public double MinHoneyToComment { get; set; }
    public MemberRole MinRoleToComment { get; set; }
}