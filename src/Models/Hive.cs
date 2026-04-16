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
    public List<Post> Posts { get; set; }
    public List<User> Followers { get; set; }

    public int PostCount { get; set; }
    public int FollowerCount { get; set; }

    [ForeignKey(nameof(Creator))]
    public int CreatorId { get; set; }
    public User? Creator { get; set; }
}