using Microsoft.EntityFrameworkCore;

namespace HiveMime.Tests;

public class PollServiceTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

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
        return context;
    }

    [Fact]
    public async Task BrowsePolls_NoPolls_ReturnsEmptyList()
    {
        // Arrange
        await using var context = CreateContext();
        var service = new PollService(context);

        // Act
        var result = service.BrowsePolls();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task BrowsePolls_WithPolls_ReturnsOnlyParentPolls()
    {
        // Arrange
        await using var context = CreateContext();
        var user = new User { Username = "testuser" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var parentPoll = new Poll { Title = "Parent Poll", Description = "Parent Poll Description", CreatorId = user.Id };
        context.Polls.Add(parentPoll);
        await context.SaveChangesAsync();

        var childPoll = new Poll { Title = "Child Poll", Description = "Child Poll Description", CreatorId = user.Id, ParentPollId = parentPoll.Id };
        context.Polls.Add(childPoll);
        await context.SaveChangesAsync();

        var service = new PollService(context);

        // Act
        var result = service.BrowsePolls();

        // Assert
        Assert.Single(result);
        Assert.Equal(parentPoll.Id, result[0].Id);
        Assert.Equal("Parent Poll", result[0].Title);
    }

    [Fact]
    public async Task CreatePoll_ValidPoll_AddsToDatabase()
    {
        // Arrange
        await using var context = CreateContext();
        var user = new User { Username = "testuser" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new PollService(context);
        var pollDto = new CreatePollDto
        {
            Title = "New Poll",
            Description = "New poll description",
            Options = new List<PollOptionDto>
            {
                new PollOptionDto { Name = "Option 1" },
                new PollOptionDto { Name = "Option 2" }
            },
            SubPolls = new()
        };

        // Act
        service.CreatePoll(user.Id, pollDto);

        // Assert
        var poll = await context.Polls.Include(p => p.Options).FirstOrDefaultAsync(p => p.Title == "New Poll");
        Assert.NotNull(poll);
        Assert.Equal("New Poll", poll.Title);
        Assert.Equal("New poll description", poll.Description);
        Assert.Equal(user.Id, poll.CreatorId);
        Assert.Equal(2, poll.Options.Count);
        Assert.Equal("Option 1", poll.Options[0].Name);
        Assert.Equal("Option 2", poll.Options[1].Name);
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
            CreatorId = user.Id,
            Options = new List<PollOption>
            {
                new PollOption { Name = "Option 1" },
                new PollOption { Name = "Option 2" }
            }
        };
        context.Polls.Add(poll);
        await context.SaveChangesAsync();

        var service = new PollService(context);

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
        var service = new PollService(context);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => service.GetPollDetails(999));
    }

    [Fact]
    public async Task UpsertVoteToPoll_NewVote_CreatesVote()
    {
        // Arrange
        await using var context = CreateContext();
        var user = new User { Username = "testuser" };
        var pollOption = new PollOption { Name = "Option 1" };
        var poll = new Poll { Title = "Test Poll", Description = "Test Poll Description", Creator = user, Options = new List<PollOption> { pollOption } };
        context.Polls.Add(poll);
        await context.SaveChangesAsync();

        var service = new PollService(context);
        var votes = new[]
        {
            new UpsertVoteToPollDto { PollOptionId = pollOption.Id, Value = 1 }
        };

        // Act
        service.UpsertVoteToPoll(user.Id, votes);

        // Assert
        var vote = await context.Votes.FirstOrDefaultAsync();
        Assert.NotNull(vote);
        Assert.Equal(user.Id, vote.UserId);
        Assert.Equal(pollOption.Id, vote.PollOptionId);
        Assert.Equal(1, vote.Value);
    }

    [Fact]
    public async Task UpsertVoteToPoll_ExistingVote_UpdatesVote()
    {
        // Arrange
        await using var context = CreateContext();
        var user = new User { Username = "testuser" };
        var pollOption = new PollOption { Name = "Option 1" };
        var poll = new Poll { Title = "Test Poll", Description = "Test Poll Description", Creator = user, Options = new List<PollOption> { pollOption } };
        var existingVote = new Vote { User = user, PollOption = pollOption, Value = 1 };
        context.Polls.Add(poll);
        context.Votes.Add(existingVote);
        await context.SaveChangesAsync();

        var service = new PollService(context);
        var votes = new[]
        {
            new UpsertVoteToPollDto { PollOptionId = pollOption.Id, Value = 2 }
        };

        // Act
        service.UpsertVoteToPoll(user.Id, votes);

        // Assert
        var vote = await context.Votes.FirstOrDefaultAsync();
        Assert.NotNull(vote);
        Assert.Equal(2, vote.Value);
    }
}
