using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace HiveMime.Tests;

public class PostServiceTests : IntegrationTest
{
    private Hive _defaultHive;
    private Post _defaultPost;
    private Post _defaultPost2;
    private User _defaultUser;
    private User _defaultUser2;

    private PostService _service;

    public PostServiceTests(DatabaseContainer fixture) : base(fixture)
    {
        _service = Context.GetService<PostService>();
    }

    [Fact]
    public async Task BrowsePosts_ByUser_ReturnsPost()
    {
        // Act
        var result = await _service.BrowsePostsAsync(_defaultPost.CreatorId, null, null, null);

        // Assert
        Assert.Single(result);
        Assert.Equal(_defaultPost.Id, result[0].Id);
    }

    [Fact]
    public async Task BrowsePosts_ByHive_ReturnsPost()
    {
        // Act
        var result = await _service.BrowsePostsAsync(null, _defaultHive.Id, null, null);

        // Assert
        Assert.Single(result);
        Assert.Equal(_defaultPost2.Id, result[0].Id);
    }

    [Fact]
    public async Task BrowsePosts_WithoutFilter_ReturnsAll()
    {
        // Act
        var result = await _service.BrowsePostsAsync(null, null, null, null);

        // Assert
        Assert.Equal(2, result.Count);

        Assert.Equal(_defaultPost.Id, result[1].Id);
        Assert.Equal(_defaultPost2.Id, result[0].Id);
    }

    [Fact]
    public async Task BrowsePosts_WithDateFilter_ReturnsExpected()
    {
        // Arrange
        Post newPost = new()
        {
            Title = "New Post",
            Description = "This is a new post.",
            Creator = _defaultUser
        };

        Context.Posts.Add(newPost);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.BrowsePostsAsync(null, null, null, newPost.CreatedAt);

        // Assert
        Assert.Equal(2, result.Count);

        Assert.Equal(_defaultPost.Id, result[1].Id);
        Assert.Equal(_defaultPost2.Id, result[0].Id);
    }

    [Fact]
    public async Task BrowsePosts_WithTextFilter_ReturnsExpected()
    {
        // Act
        var result = await _service.BrowsePostsAsync(null, null, "not a default post", null);

        // Assert
        Assert.Single(result);
        Assert.Equal(_defaultPost2.Id, result[0].Id);
    }

    [Fact]
    public async Task CreatePost_ValidPost_AddsToDatabase()
    {
        // Arrange
        var postDto = new CreatePostDto
        {
            Title = "New Post",
            Description = "New post description",
            Polls =
            [
                new CreatePollDto
                {
                    Title = "Poll 1",
                    Description = "Description 1",
                    PollType = PollType.Choice,
                    Candidates = [
                        new CreateCandidateDto { Name = "Option 1", Description = "Option 1 Description" },
                        new CreateCandidateDto { Name = "Option 2", Description = "Option 2 Description" }
                    ],
                    Categories = []
                }
            ]
        };

        // Act
        var post = await _service.CreatePostAsync(_defaultUser.Id, postDto);

        // Assert
        Assert.NotNull(post);
        Assert.Equal("New Post", post.Title);
        Assert.Equal("New post description", post.Description);
        Assert.Equal(_defaultUser.Id, post.Creator.Id);
        Assert.Single(post.Polls);
        Assert.Equal("Poll 1", post.Polls[0].Title);
        Assert.Equal("Description 1", post.Polls[0].Description);
        Assert.Equal(PollType.Choice, post.Polls[0].PollType);
        Assert.Equal(2, post.Polls[0].Candidates.Count);
        Assert.Equal("Option 1", post.Polls[0].Candidates[0].Name);
        Assert.Equal("Option 2", post.Polls[0].Candidates[1].Name);
    }

    protected override void SeedDatabase()
    {
        _defaultUser = new User { Username = "defaultuser", Settings = new() };
        _defaultUser2 = new User { Username = "defaultuser2", Settings = new() };

        _defaultHive = new Hive { Name = "Default Hive", Description = "This is a default hive.", Creator = _defaultUser };

        _defaultPost = new()
        {
            Title = "Default Post",
            Description = "This is a default post.",
            Creator = _defaultUser,
            Polls = [
                new Poll
                {
                    Title = "Default Poll",
                    Description = "This is a default poll.",
                    PollType = PollType.Choice,
                    Candidates = new List<Candidate>
                    {
                        new Candidate { Name = "Option 1", Description = "Option 1 Description" },
                        new Candidate { Name = "Option 2", Description = "Option 2 Description" }
                    }
                }
            ]
        };

        _defaultPost2 = new()
        {
            Title = "Not a default post",
            Description = "This is not a default post.",
            Creator = _defaultUser2,
            Hive = _defaultHive,
            Polls = [
                new Poll
                {
                    Title = "Not a default poll",
                    Description = "This is not a default poll.",
                    PollType = PollType.Choice,
                    Candidates = new List<Candidate>
                    {
                        new Candidate { Name = "Option 1", Description = "Option 1 Description" },
                        new Candidate { Name = "Option 2", Description = "Option 2 Description" }
                    }
                }
            ]
        };

        Context.Posts.Add(_defaultPost);
        Context.Posts.Add(_defaultPost2);
    }
}
