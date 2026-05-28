public class CommentDto : IHasIdentifier
{
    public UserDto User { get; set; }
    public MemberRole? Role { get; set; }
    public int Id { get; set; }
    public int PostId { get; set; }
    public int? ParentCommentId { get; set; }
    public string Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public int ReplyCount { get; set; }
    public bool IsOriginalPoster { get; set; }
}

public class UserHistoryCommentDto : CommentDto
{
    public CommentPostDto Post { get; set; }
}

public class CommentPostDto
{
    public string? Title { get; set; }
}