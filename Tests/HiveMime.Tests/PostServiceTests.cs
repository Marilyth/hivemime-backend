using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace HiveMime.Tests;

public class PostServiceTests : IntegrationTest
{
    private Post _defaultPost;
    private User _defaultUser;

    private PostService _service;

    public PostServiceTests(DatabaseContainer fixture) : base(fixture)
    {
        _service = Context.GetService<PostService>();
    }

    [Fact]
    public async Task BrowsePosts_WithPosts_ReturnsPosts()
    {
        // Act
        var result = await _service.BrowsePostsAsync(_defaultPost.CreatorId, null, null, null);

        // Assert
        Assert.Single(result);
        Assert.Equal(_defaultPost.Id, result[0].Id);
        Assert.Equal("Default Post", result[0].Title);
        Assert.Equal(_defaultUser.Id, result[0].Creator.Id);
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
                        new PollCandidateDto { Name = "Option 1", Description = "Option 1 Description" },
                        new PollCandidateDto { Name = "Option 2", Description = "Option 2 Description" }
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

        Context.Posts.Add(_defaultPost);
    }
}
