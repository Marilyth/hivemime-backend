using System.Linq.Expressions;

public static class CommentExtensions
{
    public static readonly Expression<Func<Comment, CommentDto>> ToDtoExpression = comment => new()
    {
        Id = comment.Id,
        PostId = comment.PostId,
        ParentCommentId = comment.ParentCommentId,
        Content = comment.Content,
        CreatedAt = comment.CreatedAt,
        UpdatedAt = comment.UpdatedAt,
        ReplyCount = comment.Replies.Count,
        User = new UserDto
        {
            Id = comment.User.Id,
            Username = comment.User.Username,
        }
    };

    public static readonly Expression<Func<Comment, UserHistoryCommentDto>> ToUserHistoryCommentDtoExpression = comment => new()
    {
        Id = comment.Id,
        PostId = comment.PostId,
        ParentCommentId = comment.ParentCommentId,
        Content = comment.Content,
        CreatedAt = comment.CreatedAt,
        UpdatedAt = comment.UpdatedAt,
        ReplyCount = comment.Replies.Count,
        User = new UserDto
        {
            Id = comment.User.Id,
            Username = comment.User.Username,
        },
        Post = new CommentPostDto
        {
            Title = comment.Post.Title
        }
    };
    
    public static Comment ToComment(this CreateCommentDto dto, int userId) => new()
    {
        PostId = dto.PostId,
        ParentCommentId = dto.ParentCommentId,
        UserId = userId,
        Content = dto.Content
    };

    public static IQueryable<CommentDto> ToDto(this IQueryable<Comment> query)
        => query.Select(ToDtoExpression);
}