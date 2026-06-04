using Mapster;
using Microsoft.EntityFrameworkCore;

public class CommentService(HiveMimeContext context, HoneyDeltaCalculator honeyDeltaCalculator, AuthorizationService authorizationService)
{
    public async Task<CommentDto> GetCommentByIdAsync(Guid commentId)
        => await context.Comments.QueryableFind(commentId).ProjectToType<CommentDto>().FirstOrExceptionAsync();

    public async Task<HoneyDeltaDto<CommentDto>> AddCommentAsync(Guid userId, CreateCommentDto dto)
    {
        await authorizationService.VerifyCreateCommentAsync(userId, dto.PostId);

        var comment = dto.Adapt<Comment>();
        comment.UserId = userId;
        context.Comments.Add(comment);

        await context.SaveChangesAsync();
        
        return await honeyDeltaCalculator.FromCommentDtoAsync(userId, comment.ToQueryable(context).ProjectToType<CommentDto>().FirstOrDefault());
    }

    public async Task<CommentDto> EditCommentAsync(Guid userId, EditCommentDto dto)
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

    public async Task DeleteCommentAsync(Guid userId, Guid commentId)
    {
        await authorizationService.VerifyDeleteCommentAsync(userId, commentId);

        var comment = await context.Comments.FindAsync(commentId);

        context.Comments.Remove(comment);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Fetches and returns comments based on the provided filters and pagination parameters.
    /// </summary>
    /// <param name="userId">The ID of the user whose comments to fetch.</param>
    /// <param name="postId">The ID of the post whose comments to fetch.</param>
    /// <param name="parentCommentId">The ID of the parent comment whose replies to fetch.</param>
    /// <param name="onlyRoot">Whether to fetch only root comments (i.e., comments without a parent).</param>
    /// <param name="pagination">The pagination parameters, including filter and order by options.</param>
    /// <returns>A list of comments matching the provided filters and pagination parameters.</returns>
    /// <exception cref="ValidationException">Thrown if none of the filters are provided.</exception>
    public async Task<PaginationResultDto<CommentDto>> BrowseCommentsAsync(Guid? userId, Guid? postId, Guid? parentCommentId, bool onlyRoot, CommentPaginationDto pagination)
    {
        if (userId == null && postId == null && parentCommentId == null)
            throw new ValidationException("At least one of userId, postId, or parentCommentId must be provided.");

        var comments = context.Comments.AsQueryable();

        if (userId.HasValue)
            comments = comments.Where(c => c.UserId == userId.Value);

        if (postId.HasValue)
            comments = comments.Where(c => c.PostId == postId.Value);

        if (parentCommentId.HasValue)
            comments = comments.Where(c => c.ParentCommentId == parentCommentId.Value);
        else if (onlyRoot)
            comments = comments.Where(c => c.ParentCommentId == null);

        return await new CommentPaginationHelper(pagination)
            .ApplyPaginationAsync<CommentDto>(comments);
    }
}