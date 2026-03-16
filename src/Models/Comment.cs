using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Comment : EntityWithIdentifier
{
    [MaxLength(1024)]
    public string Content { get; set; }
    public List<Comment> Replies { get; set; }

    [ForeignKey(nameof(User))]
    public int UserId { get; set; }
    public User? User { get; set; }

    [ForeignKey(nameof(Post))]
    public int PostId { get; set; }
    public Post? Post { get; set; }

    [ForeignKey(nameof(ParentComment))]
    public int? ParentCommentId { get; set; }
    public Comment? ParentComment { get; set; }
}