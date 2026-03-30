using Mapster;
using Microsoft.EntityFrameworkCore;

public class CommentService(HiveMimeContext context)
{
    public async Task<CommentDto> AddCommentAsync(int userId, CreateCommentDto dto)
    {
        var comment = dto.Adapt<Comment>();
        comment.UserId = userId;
        context.Comments.Add(comment);

        await context.SaveChangesAsync();
        
        return comment.ToQueryable(context).ProjectToType<CommentDto>().FirstOrDefault();
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

        return comment.ToQueryable(context).ProjectToType<CommentDto>().FirstOrDefault();
    }

    public async Task DeleteCommentAsync(int userId, int commentId)
    {
        var comment = await context.Comments.FindAsync(commentId);

        if (comment == null || comment.UserId != userId)
            throw new UnauthorizedAccessException("You do not have permission to delete this comment.");

        context.Comments.Remove(comment);
        await context.SaveChangesAsync();
    }

    public async Task<List<CommentDto>> GetCommentsByUserAsync(int userId, DateTimeOffset? beforeDate)
    {
        return await GetComments(userId, null, null, beforeDate).ProjectToType<CommentDto>().ToListAsync();
    }

    public async Task<List<CommentDto>> GetCommentsByPostAsync(int postId, int? parentCommentId, DateTimeOffset? beforeDate)
    {
        return await GetComments(null, postId, parentCommentId, beforeDate).ProjectToType<CommentDto>().ToListAsync();
    }

    public IQueryable<Comment> GetComments(int? userId, int? postId, int? parentCommentId, DateTimeOffset? beforeDate)
    {
        var comments = context.Comments.AsQueryable();

        if (userId.HasValue)
            comments = comments.Where(c => c.UserId == userId.Value);

        if (postId.HasValue)
            comments = comments.Where(c => c.PostId == postId.Value && c.ParentCommentId == parentCommentId);

        if (beforeDate.HasValue)
            comments = comments.Where(c => c.CreatedAt < beforeDate.Value);

        return comments.OrderByDescending(c => c.CreatedAt).Take(20);
    }
}