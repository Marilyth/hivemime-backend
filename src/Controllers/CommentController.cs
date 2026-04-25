using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class CommentController(CommentService commentService, HiveMimeContext context) : ControllerBase
{
    [HttpPost("create")]
    public async Task<HoneyDeltaDto<CommentDto>> CreateComment(CreateCommentDto dto)
        => await commentService.AddCommentAsync(await User.GetUserIdAsync(context), dto);

    [HttpPut("edit")]
    public async Task<CommentDto> EditComment(EditCommentDto dto)
        => await commentService.EditCommentAsync(await User.GetUserIdAsync(context), dto);

    [HttpDelete("delete")]
    public async Task DeleteComment(int commentId)
        => await commentService.DeleteCommentAsync(await User.GetUserIdAsync(context), commentId);

    [HttpGet("getByPost")]
    public async Task<List<CommentDto>> GetCommentsByPost(int? userId, int? postId, int? parentCommentId, CommentPaginationDto pagination)
        => await commentService.GetCommentsAsync(userId, postId, parentCommentId, pagination);
}