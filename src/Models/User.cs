using System.ComponentModel.DataAnnotations;

public class User : EntityWithIdentifier
{
    public string Username { get; set; }
    public string? Email { get; set; }
    public DateTimeOffset? DateOfBirth { get; set; }

    public List<CandidateVote> Votes { get; set; }
    public List<Post> CreatedPosts { get; set; }
    public List<Comment> Comments { get; set; }
    public List<Hive> FollowedHives { get; set; }
    public UserSettings Settings { get; set; }
}