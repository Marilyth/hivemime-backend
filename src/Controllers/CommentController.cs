using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

[ApiController]
[Route("api/[controller]")]
public class CommentController(CommentService commentService, HiveMimeContext context) : ControllerBase
{
    [HttpGet("get")]
    [EnableRateLimiting("1/1s")]
    public async Task<CommentDto> GetComment(Guid commentId)
        => await commentService.GetCommentByIdAsync(commentId);

    [HttpPost("create")]
    [EnableRateLimiting("1/5s")]
    public async Task<HoneyDeltaDto<CommentDto>> CreateComment(CreateCommentDto dto)
        => await commentService.AddCommentAsync(await User.GetUserIdAsync(context), dto);

    [HttpPatch("edit")]
    [EnableRateLimiting("1/5s")]
    public async Task<CommentDto> EditComment(EditCommentDto dto)
        => await commentService.EditCommentAsync(await User.GetUserIdAsync(context), dto);

    [HttpDelete("delete")]
    [EnableRateLimiting("1/5s")]
    public async Task DeleteComment(Guid commentId)
        => await commentService.DeleteCommentAsync(await User.GetUserIdAsync(context), commentId);

    [HttpPost("browse")]
    [EnableRateLimiting("1/1s")]
    public async Task<PaginationResultDto<CommentDto>> GetComments(Guid? userId, Guid? postId, Guid? parentCommentId, bool onlyRoot, [FromBody] CommentPaginationDto pagination)
        => await commentService.BrowseCommentsAsync(userId, postId, parentCommentId, onlyRoot, pagination);
}