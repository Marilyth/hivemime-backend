using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

[Index(nameof(FirebaseId), IsUnique = true)]
[Index(nameof(Username), IsUnique = true)]
public class User : RootEntity
{
    public string? FirebaseId { get; set; }
    public bool IsAnonymous { get; set; }
    public bool IsVerified { get; set; }

    [MaxLength(64)]
    public string Username { get; set; }
    public double Honey { get; set; }

    public DateTimeOffset? DateOfBirth { get; set; }
    public DateTimeOffset LastLogin { get; set; } = DateTimeOffset.UtcNow;

    public List<PostVote> Votes { get; set; }
    public List<Post> CreatedPosts { get; set; }
    public List<Comment> Comments { get; set; }
    public List<HiveUser> JoinedHives { get; set; }
    public UserSettings Settings { get; set; }
}