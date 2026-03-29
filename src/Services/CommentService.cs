using Microsoft.EntityFrameworkCore;

public class CommentService(HiveMimeContext context)
{
    public async Task<CommentDto> AddCommentAsync(int userId, CreateCommentDto dto)
    {
        var comment = dto.ToComment(userId);
        context.Comments.Add(comment);

        await context.SaveChangesAsync();

        comment.User = await context.Users.FindAsync(userId);
        
        return comment.ToDto();
    }

    public async Task<CommentDto> EditCommentAsync(int userId, EditCommentDto dto)
    {
        var comment = await context.Comments.Where(c => c.Id == dto.CommentId)
            .Include(c => c.User)
            .FirstOrDefaultAsync();

        if (comment == null || comment.UserId != userId)
            throw new UnauthorizedAccessException("You do not have permission to edit this comment.");

        comment.Content = dto.NewContent;
        comment.UpdatedAt = DateTime.UtcNow;

        context.Comments.Update(comment);
        await context.SaveChangesAsync();

        return comment.ToDto();
    }

    public async Task DeleteCommentAsync(int userId, int commentId)
    {
        var comment = await context.Comments.FindAsync(commentId);

        if (comment == null || comment.UserId != userId)
            throw new UnauthorizedAccessException("You do not have permission to delete this comment.");

        context.Comments.Remove(comment);
        await context.SaveChangesAsync();
    }

    public async Task<List<CommentDto>> GetCommentsAsync(int postId, int? parentCommentId, DateTimeOffset? beforeDate)
    {
        var comments = context.Comments
            .Where(c => c.PostId == postId && c.ParentCommentId == parentCommentId);

        if (beforeDate.HasValue)
            comments = comments.Where(c => c.CreatedAt < beforeDate.Value);

        return await comments.OrderByDescending(c => c.CreatedAt).Take(20).ToDto().ToListAsync();
    }
}