using System.ComponentModel.DataAnnotations;

public class User : EntityWithIdentifier
{
    public string Username { get; set; }
    public string? Email { get; set; }

    // Demographic data which is optionally included in votes based on user settings.
    [MaxLength(100)]
    public string? Country { get; set; }
    public DateTimeOffset? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }

    public List<CandidateVote> Votes { get; set; }
    public List<Post> CreatedPosts { get; set; }
    public List<Comment> Comments { get; set; }
    public List<Hive> FollowedHives { get; set; }
    public UserSettings Settings { get; set; }
}

public enum Gender
{
    Male,
    Female,
    NonBinary
}