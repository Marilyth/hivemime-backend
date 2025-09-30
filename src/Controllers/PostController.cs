using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HiveMime.Controllers;

[ApiController]
[Route("api/post")]
public class PostController(IPostService postService) : ControllerBase
{
    [HttpGet("browse")]
    public List<ListPostDto> BrowsePosts()
    {
        return postService.BrowsePosts(User.GetUserId());
    }

    [HttpPost("create")]
    [Authorize]
    public void CreatePost([FromBody] CreatePostDto postDto)
    {
        postService.CreatePost(User.GetUserId(), postDto);
    }
}
