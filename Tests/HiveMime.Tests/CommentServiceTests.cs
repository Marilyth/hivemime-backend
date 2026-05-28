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
        var postFeed = await _service.BrowseCommentsAsync(null, _defaultPost.Id, null, true, new CommentPaginationDto { PageSize = 20 });
        var commentFeed = await _service.BrowseCommentsAsync(null, _defaultPost.Id, _defaultComment.Id, false, new CommentPaginationDto { PageSize = 20 });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_defaultUser!.Id, result.Dto.User.Id);
        Assert.Equal("Newly added reply", result.Dto.Content);
        Assert.DoesNotContain(postFeed.Items, c => c.Id == result.Dto.Id);
        Assert.Contains(commentFeed.Items, c => c.Id == result.Dto.Id);
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
        Assert.Equal(_defaultUser!.Id, result.Dto.User.Id);
        Assert.Equal("Newly added comment", result.Dto.Content);
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
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteCommentAsync(otherUser.Id, _defaultComment!.Id));
    }

    [Fact]
    public async Task AddCommentAsync_HivePostByOutsider_AddsComment()
    {
        // Arrange
        var outsider = new User { Username = "outsider", Settings = new() };
        var hive = new Hive
        {
            Name = "comment-hive",
            Description = "desc",
            Settings = new(),
            Users = [new() { User = _defaultUser!, Role = MemberRole.Creator, ApprovalStatus = ApprovalStatus.Approved }]
        };
        var hivePost = new Post { Creator = _defaultUser!, Hive = hive, ApprovalStatus = ApprovalStatus.Approved, Polls = [] };
        Context.Users.Add(outsider);
        Context.Posts.Add(hivePost);
        await Context.SaveChangesAsync();

        var dto = new CreateCommentDto
        {
            PostId = hivePost.Id,
            Content = "blocked"
        };

        // Act
        var result = await _service.AddCommentAsync(outsider.Id, dto);

        // Assert
        Assert.Equal(outsider.Id, result.Dto.User.Id);
        Assert.Equal("blocked", result.Dto.Content);
    }

    [Fact]
    public async Task AddCommentAsync_PublicPostByOutsider_AddsComment()
    {
        // Arrange
        var outsider = new User { Username = "public-outsider", Settings = new() };
        var publicPost = new Post { Creator = _defaultUser!, ApprovalStatus = ApprovalStatus.Approved, Polls = [] };
        Context.Users.Add(outsider);
        Context.Posts.Add(publicPost);
        await Context.SaveChangesAsync();

        var dto = new CreateCommentDto
        {
            PostId = publicPost.Id,
            Content = "allowed"
        };

        // Act
        var result = await _service.AddCommentAsync(outsider.Id, dto);

        // Assert
        Assert.Equal(outsider.Id, result.Dto.User.Id);
        Assert.Equal("allowed", result.Dto.Content);
    }

    [Fact]
    public async Task DeleteCommentAsync_HiveModerator_DeletesComment()
    {
        // Arrange
        var moderator = new User { Username = "mod", Settings = new() };
        var author = new User { Username = "comment-author", Settings = new() };
        var hive = new Hive
        {
            Name = "delete-comment-hive",
            Description = "desc",
            Settings = new(),
            Users =
            [
                new() { User = _defaultUser!, Role = MemberRole.Creator, ApprovalStatus = ApprovalStatus.Approved },
                new() { User = moderator, Role = MemberRole.Moderator, ApprovalStatus = ApprovalStatus.Approved },
                new() { User = author, Role = MemberRole.Follower, ApprovalStatus = ApprovalStatus.Approved }
            ]
        };
        var post = new Post { Creator = _defaultUser!, Hive = hive, ApprovalStatus = ApprovalStatus.Approved, Polls = [] };
        var comment = new Comment { Post = post, User = author, Content = "to delete" };

        Context.Comments.Add(comment);
        await Context.SaveChangesAsync();

        // Act
        await _service.DeleteCommentAsync(moderator.Id, comment.Id);

        // Assert
        Assert.Null(await Context.Comments.FindAsync(comment.Id));
    }

    [Fact]
    public async Task GetCommentsAsync_WithComments_ReturnsComments()
    {
        // Arrange
        Context.ChangeTracker.Clear();

        // Act
        var comments = await _service.BrowseCommentsAsync(null, _defaultPost!.Id, null, true, new CommentPaginationDto { PageSize = 20 });

        // Assert
        Assert.Single(comments.Items);
        Assert.Equal(_defaultComment!.Content, comments.Items[0].Content);
        Assert.Equal(_defaultUser!.Id, comments.Items[0].User.Id);
    }

    [Fact]
    public async Task GetCommentsAsync_Pagination_WorksWithFilterAndOrder()
    {
        // Arrange
        var comment1 = new Comment { Post = _defaultPost, User = _defaultUser, Content = "Alpha comment" };
        var comment2 = new Comment { Post = _defaultPost, User = _defaultUser, Content = "Beta comment" };
        Context.Comments.AddRange(comment1, comment2);
        await Context.SaveChangesAsync();
        var pagination = new CommentPaginationDto { Filter = "Alpha", OrderBy = CommentOrderBy.New, PageSize = 20 };

        // Act
        var comments = await _service.BrowseCommentsAsync(null, _defaultPost!.Id, null, true, pagination);

        // Assert
        Assert.Single(comments.Items);
        Assert.Contains("Alpha", comments.Items[0].Content);
    }

    protected override void SeedDatabase()
    {
        _defaultUser = new User { Username = "defaultuser", Settings = new() };
        _defaultPost = new Post
        {
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
