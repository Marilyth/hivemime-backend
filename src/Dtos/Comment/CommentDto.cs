public class CommentDto
{
    public UserDto User { get; set; }
    public int Id { get; set; }
    public int? ParentCommentId { get; set; }
    public string Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}