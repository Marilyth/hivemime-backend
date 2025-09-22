public class User : EntityWithIdentifier
{
    public string Username { get; set; }
    public string? Email { get; set; }

    public List<Vote> Votes { get; set; }
    public List<Post> CreatedPosts { get; set; }
    public List<Comment> Comments { get; set; }
    public List<Tag> FollowedTags { get; set; }
}