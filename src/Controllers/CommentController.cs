using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class CommentController(CommentService commentService) : ControllerBase
{
    [HttpPost("create")]
    public async Task<CommentDto> CreateComment(CreateCommentDto dto)
        => await commentService.AddCommentAsync(User.GetUserId(), dto);

    [HttpPut("edit")]
    public async Task<CommentDto> EditComment(EditCommentDto dto)
        => await commentService.EditCommentAsync(User.GetUserId(), dto);

    [HttpDelete("delete")]
    public async Task DeleteComment(int commentId)
        => await commentService.DeleteCommentAsync(User.GetUserId(), commentId);

    [HttpGet("get")]
    public async Task<List<CommentDto>> GetCommentsForPost(int postId)
        => await commentService.GetCommentsForPostAsync(postId);
}