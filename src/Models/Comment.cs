using System.ComponentModel.DataAnnotations.Schema;

public class Comment : EntityWithIdentifier
{
    public string Content { get; set; }
    public int Likes { get; set; }
    public int Dislikes { get; set; }
    public List<Comment> Replies { get; set; }

    [ForeignKey(nameof(User))]
    public int UserId { get; set; }
    public User? User { get; set; }

    [ForeignKey(nameof(Poll))]
    public int PollId { get; set; }
    public Poll? Poll { get; set; }

    [ForeignKey(nameof(ParentComment))]
    public int? ParentCommentId { get; set; }
    public Comment? ParentComment { get; set; }
}