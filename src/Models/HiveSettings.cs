using Microsoft.EntityFrameworkCore;

[Owned]
public class HiveSettings
{
    public bool IsPrivate { get; set; }
    public bool MustBeApprovedToPost { get; set; }
    public bool MustBeApprovedToJoin { get; set; }

    public double MinHoneyToPost { get; set; }
    public MemberRole? MinRoleToPost { get; set; }
}