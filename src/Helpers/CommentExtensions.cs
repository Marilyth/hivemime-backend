using System.Linq.Expressions;

public static class CommentExtensions
{
    public static readonly Expression<Func<Comment, CommentDto>> ToDtoExpression = comment => new()
        {
            Id = comment.Id,
            PostId = comment.PostId,
            ParentCommentId = comment.ParentCommentId,
            UserId = comment.UserId,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt
        };

    private static readonly Func<Comment, CommentDto> ToDtoFunc = ToDtoExpression.Compile();
    
    public static Comment ToComment(this CreateCommentDto dto, int userId) => new()
        {
            PostId = dto.PostId,
            ParentCommentId = dto.ParentCommentId,
            UserId = userId,
            Content = dto.Content
        };

    public static CommentDto ToDto(this Comment comment)
        => ToDtoFunc(comment);

    public static IQueryable<CommentDto> ToDto(this IQueryable<Comment> query)
        => query.Select(ToDtoExpression);
}