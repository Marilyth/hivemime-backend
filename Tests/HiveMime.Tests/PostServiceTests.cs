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
            Polls = new List<Poll>()
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
        var result = service.BrowsePosts();

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
                    AllowCustomAnswer = false,
                    AnswerType = PollAnswerType.SingleChoice,
                    Options = new List<PollOptionDto>
                    {
                        new PollOptionDto { Name = "Option 1", Description = "Option 1 Description" },
                        new PollOptionDto { Name = "Option 2", Description = "Option 2 Description" }
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
        Assert.Equal(PollAnswerType.SingleChoice, post.Polls[0].AnswerType);
        Assert.Equal(2, post.Polls[0].Options.Count);
        Assert.Equal("Option 1", post.Polls[0].Options[0].Name);
        Assert.Equal("Option 2", post.Polls[0].Options[1].Name);
    }

    [Fact]
    public async Task GetPollDetails_ExistingPoll_ReturnsCorrectDetails()
    {
        // Arrange
        await using var context = CreateContext();
        var user = new User { Username = "testuser" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var poll = new Poll
        {
            Title = "Test Poll",
            Description = "Test Poll Description",
            PostId = _defaultPost.Id,
            Options = new List<PollOption>
            {
                new PollOption { Name = "Option 1" },
                new PollOption { Name = "Option 2" }
            }
        };
        context.Polls.Add(poll);
        await context.SaveChangesAsync();

        var service = new PostService(context);

        // Act
        var result = service.GetPollDetails(poll.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Poll", result.Title);
        Assert.Equal(2, result.PollOption.Count);
        Assert.Equal("Option 1", result.PollOption[0].Name);
        Assert.Equal(0, result.PollOption[0].VoterAmount);
    }

    [Fact]
    public async Task GetPollDetails_NonExistentPoll_ThrowsException()
    {
        // Arrange
        await using var context = CreateContext();
        var service = new PostService(context);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => service.GetPollDetails(999));
    }

    [Fact]
    public async Task UpsertVoteToPoll_NewVote_CreatesVote()
    {
        // Arrange
        await using var context = CreateContext();
        var pollOption = new PollOption { Name = "Option 1" };
        var poll = new Poll {
            Title = "Test Poll",
            PostId = _defaultPost.Id,
            Description = "Test Poll Description",
            Options = new List<PollOption> { pollOption }
        };
        context.Polls.Add(poll);
        await context.SaveChangesAsync();

        var service = new PostService(context);
        var votes = new[]
        {
            new UpsertVoteToPollDto { PollOptionId = pollOption.Id, Value = 1 }
        };

        // Act
        service.UpsertVoteToPoll(_defaultPost.CreatorId, votes);

        // Assert
        var vote = await context.Votes.FirstOrDefaultAsync();
        Assert.NotNull(vote);
        Assert.Equal(_defaultPost.CreatorId, vote.UserId);
        Assert.Equal(pollOption.Id, vote.PollOptionId);
        Assert.Equal(1, vote.Value);
    }

    [Fact]
    public async Task UpsertVoteToPoll_ExistingVote_UpdatesVote()
    {
        // Arrange
        await using var context = CreateContext();
        var pollOption = new PollOption { Name = "Option 1" };
        var poll = new Poll {
            Title = "Test Poll",
            Description = "Test Poll Description",
            PostId = _defaultPost.Id,
            Options = new List<PollOption> { pollOption }
        };
        var existingVote = new Vote { User = _defaultPost.Creator, PollOption = pollOption, Value = 1 };
        context.Polls.Add(poll);
        context.Votes.Add(existingVote);
        await context.SaveChangesAsync();

        var service = new PostService(context);
        var votes = new[]
        {
            new UpsertVoteToPollDto { PollOptionId = pollOption.Id, Value = 2 }
        };

        // Act
        service.UpsertVoteToPoll(_defaultPost.CreatorId, votes);

        // Assert
        var vote = await context.Votes.FirstOrDefaultAsync();
        Assert.NotNull(vote);
        Assert.Equal(2, vote.Value);
    }
}
