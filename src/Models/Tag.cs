using Microsoft.EntityFrameworkCore;

[Index(nameof(Name), IsUnique = true)]
public class Tag : EntityWithIdentifier
{
    public string Name { get; set; }
    public List<Poll> Polls { get; set; }
    public List<User> Followers { get; set; }
}