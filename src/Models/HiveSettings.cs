using Microsoft.EntityFrameworkCore;

[Owned]
public class HiveSettings
{
    public bool IsPrivate { get; set; }
    public bool MustBeApprovedToPost { get; set; }
    public bool MustBeApprovedToJoin { get; set; }

    public double MinHoneyToPost { get; set; }
    public PostPolicy PostPolicy { get; set; }
}

public enum PostPolicy
{
    Anyone,
    FollowersOnly,
    ModeratorsOnly
}