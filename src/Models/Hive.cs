using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[Index(nameof(Name), IsUnique = true)]
public class Hive : EntityWithIdentifier
{
    [MaxLength(64)]
    public string Name { get; set; }

    [MaxLength(1024)]
    public string Description { get; set; }
    public HiveSettings Settings { get; set; } = new();

    public List<Post> Posts { get; set; }
    public List<HiveUser> Users { get; set; }

    public int PostCount { get; set; }
    public int UserCount { get; set; }
}

[Index(nameof(UserId), nameof(HiveId), IsUnique = true)]
public class HiveUser : EntityWithIdentifier
{
    [ForeignKey(nameof(User))]
    public int UserId { get; set; }
    public User User { get; set; }

    [ForeignKey(nameof(Hive))]
    public int HiveId { get; set; }
    public Hive Hive { get; set; }

    public ApprovalStatus ApprovalStatus { get; set; }
    public MemberRole Role { get; set; }
}

public enum MemberRole
{
    Guest,
    Follower,
    Moderator,
    Admin,
    Creator
}

public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected,
    Banned
}