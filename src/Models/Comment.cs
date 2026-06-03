using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;

public class Comment : EntityWithIdentifier
{
    [MaxLength(1024)]
    public string Content { get; set; }
    public NpgsqlTsVector SearchVector { get; set; }
    
    public List<Comment> Replies { get; set; } = [];

    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }
    public User? User { get; set; }

    [ForeignKey(nameof(Post))]
    public Guid PostId { get; set; }
    public Post? Post { get; set; }

    [ForeignKey(nameof(ParentComment))]
    public Guid? ParentCommentId { get; set; }
    public Comment? ParentComment { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}