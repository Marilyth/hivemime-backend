using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace HiveMime.Tests;

public class CommentServiceTests : IntegrationTest
{
    private CommentService _service;
    private User? _defaultUser;
    private Post? _defaultPost;
    private Comment? _defaultComment;

    public CommentServiceTests(DatabaseContainer fixture) : base(fixture)
    {
        _service = Context.GetService<CommentService>();
    }

    [Fact]
    public async Task AddCommentAsync_Reply_AddsToDatabase()
    {
        // Arrange
        var dto = new CreateCommentDto
        {
            PostId = _defaultPost!.Id,
            ParentCommentId = _defaultComment!.Id,
            Content = "Newly added reply"
        };

        // Act
        var result = await _service.AddCommentAsync(_defaultUser!.Id, dto);
        var postFeed = await _service.GetCommentsAsync(_defaultPost.Id, null, null);
        var commentFeed = await _service.GetCommentsAsync(_defaultPost.Id, _defaultComment.Id, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_defaultUser!.Id, result.User.Id);
        Assert.Equal("Newly added reply", result.Content);
        Assert.DoesNotContain(postFeed, c => c.Id == result.Id);
        Assert.Contains(commentFeed, c => c.Id == result.Id);
    }

    [Fact]
    public async Task AddCommentAsync_ValidComment_AddsToDatabase()
    {
        // Arrange
        var dto = new CreateCommentDto
        {
            PostId = _defaultPost!.Id,
            Content = "Newly added comment"
        };

        // Act
        var result = await _service.AddCommentAsync(_defaultUser!.Id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_defaultUser!.Id, result.User.Id);
        Assert.Equal("Newly added comment", result.Content);
    }

    [Fact]
    public async Task EditCommentAsync_ValidEdit_UpdatesContent()
    {
        // Arrange
        var dto = new EditCommentDto
        {
            CommentId = _defaultComment!.Id,
            NewContent = "Edited content"
        };

        Context.ChangeTracker.Clear();

        // Act
        var result = await _service.EditCommentAsync(_defaultUser!.Id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Edited content", result.Content);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task EditCommentAsync_UnauthorizedUser_ThrowsException()
    {
        // Arrange
        var dto = new EditCommentDto
        {
            CommentId = _defaultComment!.Id,
            NewContent = "Edited content"
        };

        var otherUser = new User { Username = "otheruser", Settings = new() };
        Context.Users.Add(otherUser);
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.EditCommentAsync(otherUser.Id, dto));
    }

    [Fact]
    public async Task DeleteCommentAsync_ValidUser_DeletesComment()
    {
        // Arrange
        Context.ChangeTracker.Clear();

        // Act
        await _service.DeleteCommentAsync(_defaultUser!.Id, _defaultComment!.Id);
        var deleted = await Context.Comments.FindAsync(_defaultComment.Id);

        // Assert
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteCommentAsync_UnauthorizedUser_ThrowsException()
    {
        // Arrange
        var otherUser = new User { Username = "otheruser", Settings = new() };
        Context.Users.Add(otherUser);
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.DeleteCommentAsync(otherUser.Id, _defaultComment!.Id));
    }

    [Fact]
    public async Task GetCommentsAsync_WithComments_ReturnsComments()
    {
        // Arrange
        Context.ChangeTracker.Clear();

        // Act
        var comments = await _service.GetCommentsAsync(_defaultPost!.Id, null, null);

        // Assert
        Assert.Single(comments);
        Assert.Equal(_defaultComment!.Content, comments[0].Content);
        Assert.Equal(_defaultUser!.Id, comments[0].User.Id);
    }
    
    [Fact]
    public async Task GetCommentsAsync_WithCursor_ReturnsExpected()
    {
        // Arrange
        Comment newComment = new()
        {
            Post = _defaultPost,
            User = _defaultUser,
            Content = "Test comment"
        };

        Context.Comments.Add(newComment);
        await Context.SaveChangesAsync();

        // Act
        var comments = await _service.GetCommentsAsync(_defaultPost!.Id, null, newComment.CreatedAt);

        // Assert
        Assert.Single(comments);
        Assert.Equal(newComment.Content, comments[0].Content);
    }

    protected override void SeedDatabase()
    {
        _defaultUser = new User { Username = "defaultuser", Settings = new() };
        _defaultPost = new Post
        {
            Title = "Default Post",
            Creator = _defaultUser,
            Comments = [],
            Polls = [],
            PostVotes = []
        };
        _defaultComment = new Comment
        {
            Post = _defaultPost,
            User = _defaultUser,
            Content = "Test comment"
        };

        Context.Comments.Add(_defaultComment);
        Context.SaveChanges();
    }
}
