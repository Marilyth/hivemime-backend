public class CommentDto
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public int? ParentCommentId { get; set; }
    public int UserId { get; set; }
    public string Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}