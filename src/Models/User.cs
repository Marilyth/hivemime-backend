using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

[Index(nameof(FirebaseId), IsUnique = true)]
[Index(nameof(Username), IsUnique = true)]
public class User : EntityWithIdentifier
{
    public string? FirebaseId { get; set; }
    public bool IsAnonymous { get; set; }
    public bool IsVerified { get; set; }

    [MaxLength(64)]
    public string Username { get; set; }
    [MaxLength(256)]
    public string? Email { get; set; }

    public DateTimeOffset? DateOfBirth { get; set; }
    public DateTimeOffset LastLogin { get; set; } = DateTimeOffset.UtcNow;

    public List<PostVote> Votes { get; set; }
    public List<Post> CreatedPosts { get; set; }
    public List<Comment> Comments { get; set; }
    public List<Hive> FollowedHives { get; set; }
    public List<Hive> CreatedHives { get; set; }
    public UserSettings Settings { get; set; }
}