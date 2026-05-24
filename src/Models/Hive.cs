using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[Index(nameof(Name), IsUnique = true)]
public class Hive : EntityWithIdentifier
{
    [MaxLength(128)]
    public string Name { get; set; }

    [MaxLength(1024)]
    public string Description { get; set; }
    public HiveSettings Settings { get; set; } = new();

    public List<Post> Posts { get; set; }
    public List<HiveFollower> Followers { get; set; }
    public List<User> Moderators { get; set; }

    public int PostCount { get; set; }
    public int FollowerCount { get; set; }

    [ForeignKey(nameof(Creator))]
    public int CreatorId { get; set; }
    public User? Creator { get; set; }
}

[Index(nameof(UserId), nameof(HiveId), IsUnique = true)]
public class HiveFollower : EntityWithIdentifier
{
    [ForeignKey(nameof(User))]
    public int UserId { get; set; }
    public User User { get; set; }

    [ForeignKey(nameof(Hive))]
    public int HiveId { get; set; }
    public Hive Hive { get; set; }

    public bool IsApproved { get; set; }
}