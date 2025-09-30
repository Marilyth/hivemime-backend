using System.ComponentModel.DataAnnotations.Schema;

public class Post : EntityWithIdentifier
{
    public string Title { get; set; }
    public string? Description { get; set; }

    public List<Poll> Polls { get; set; }
    public List<Comment> Comments { get; set; }
    public List<Tag> Tags { get; set; }

    [ForeignKey(nameof(Creator))]
    public int CreatorId { get; set; }
    public User? Creator { get; set; }

    // TODO: Add hives (communities)
}