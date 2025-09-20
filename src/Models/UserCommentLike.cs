using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[PrimaryKey(nameof(UserId), nameof(CommentId))]
public class UserCommentLike : Like
{
    [ForeignKey(nameof(Comment))]
    public int CommentId { get; set; }
    public Comment? Comment { get; set; }
}