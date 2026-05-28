using Microsoft.EntityFrameworkCore;

[Owned]
public class HiveSettings
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