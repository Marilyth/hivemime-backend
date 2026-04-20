using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class CommentController(CommentService commentService, HiveMimeContext context) : ControllerBase
{
    [HttpPost("create")]
    public async Task<CommentDto> CreateComment(CreateCommentDto dto)
        => await commentService.AddCommentAsync(await User.GetUserIdAsync(context), dto);

    [HttpPut("edit")]
    public async Task<CommentDto> EditComment(EditCommentDto dto)
        => await commentService.EditCommentAsync(await User.GetUserIdAsync(context), dto);

    [HttpDelete("delete")]
    public async Task DeleteComment(int commentId)
        => await commentService.DeleteCommentAsync(await User.GetUserIdAsync(context), commentId);

    [HttpGet("getByPost")]
    public async Task<List<CommentDto>> GetCommentsByPost(int postId, int? parentCommentId, DateTimeOffset? beforeDate)
        => await commentService.GetCommentsByPostAsync(postId, parentCommentId, beforeDate);

    [HttpGet("getByUser")]
    public async Task<List<CommentDto>> GetCommentsByUser(int userId, DateTimeOffset? beforeDate)
        => await commentService.GetCommentsByUserAsync(userId, beforeDate);
}