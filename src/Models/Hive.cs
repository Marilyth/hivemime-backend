using Microsoft.EntityFrameworkCore;

[Index(nameof(Name), IsUnique = true)]
public class Hive : EntityWithIdentifier
{
    public string Name { get; set; }
    public string Description { get; set; }
    public List<Post> Posts { get; set; }
    public List<User> Followers { get; set; }
}