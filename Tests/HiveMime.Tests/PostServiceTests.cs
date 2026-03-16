using Microsoft.EntityFrameworkCore;

namespace HiveMime.Tests;

public class PollServiceTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private Post _defaultPost;

    public PollServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private HiveMimeContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HiveMimeContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        var context = new HiveMimeContext(options);
        // Since we are using the same database for all tests in this class,
        // we need to clean up the database before each test.
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        SeedDatabase(context);

        return context;
    }

    private void SeedDatabase(HiveMimeContext context)
    {
        _defaultPost = new()
        {
            Title = "Default Post",
            Description = "This is a default post.",
            Creator = new User { Username = "defaultuser" },
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

        context.Posts.Add(_defaultPost);
        context.SaveChanges();
    }

    [Fact]
    public async Task BrowsePosts_WithPosts_ReturnsPosts()
    {
        // Arrange
        await using var context = CreateContext();

        var service = new PostService(context);

        // Act
        var result = service.BrowsePosts(_defaultPost.CreatorId, null, null, null);

        // Assert
        Assert.Single(result);
        Assert.Equal(_defaultPost.Id, result[0].Id);
        Assert.Equal("Default Post", result[0].Title);
    }

    [Fact]
    public async Task CreatePost_ValidPost_AddsToDatabase()
    {
        // Arrange
        await using var context = CreateContext();
        var user = new User { Username = "testuser" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new PostService(context);
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
                    Candidates = new List<PollCandidateDto>
                    {
                        new PollCandidateDto { Name = "Option 1", Description = "Option 1 Description" },
                        new PollCandidateDto { Name = "Option 2", Description = "Option 2 Description" }
                    }
                }
            ]
        };

        // Act
        service.CreatePost(user.Id, postDto);

        // Assert
        var post = await context.Posts.Include(p => p.Polls).FirstOrDefaultAsync(p => p.Title == "New Post");
        Assert.NotNull(post);
        Assert.Equal("New Post", post.Title);
        Assert.Equal("New post description", post.Description);
        Assert.Equal(user.Id, post.CreatorId);
        Assert.Single(post.Polls);
        Assert.Equal("Poll 1", post.Polls[0].Title);
        Assert.Equal("Description 1", post.Polls[0].Description);
        Assert.Equal(PollType.Choice, post.Polls[0].PollType);
        Assert.Equal(2, post.Polls[0].Candidates.Count);
        Assert.Equal("Option 1", post.Polls[0].Candidates[0].Name);
        Assert.Equal("Option 2", post.Polls[0].Candidates[1].Name);
    }
}
