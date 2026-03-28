public class CreateCommentDto
{
    public int PostId { get; set; }
    public int? ParentCommentId { get; set; }
    public string Content { get; set; }
}
