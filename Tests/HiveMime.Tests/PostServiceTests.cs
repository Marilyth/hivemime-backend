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
                    AllowCustomAnswer = false,
                    PollType = PollType.SingleChoice,
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
        var result = service.BrowsePosts(_defaultPost.CreatorId);

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
                    PollType = PollType.SingleChoice,
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
        Assert.Equal(PollType.SingleChoice, post.Polls[0].PollType);
        Assert.Equal(2, post.Polls[0].Candidates.Count);
        Assert.Equal("Option 1", post.Polls[0].Candidates[0].Name);
        Assert.Equal("Option 2", post.Polls[0].Candidates[1].Name);
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
            Candidates = new List<Candidate>
            {
                new Candidate { Name = "Option 1" },
                new Candidate { Name = "Option 2" }
            }
        };
        context.Polls.Add(poll);
        await context.SaveChangesAsync();

        var service = new PostService(context);

        // Act
        var result = service.GetPollDetails(poll.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Candidates.Count);
        Assert.Equal("Option 1", result.Candidates[0].Name);
        Assert.Equal(0, result.Candidates[0].VoterAmount);
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
    public async Task UpsertVoteToPost_NewVote_CreatesVote()
    {
        // Arrange
        await using var context = CreateContext();
        var createVoteDto = new UpsertVoteToPostDto()
        {
            PostId = _defaultPost.Id,
            Polls = [
                new UpsertVoteToPollDto
                {
                    PollId = _defaultPost.Polls[0].Id,
                    Candidates = [
                        new UpsertVoteToCandidateDto {
                            CandidateId = _defaultPost.Polls[0].Candidates[0].Id,
                            Value = 1
                        }
                    ]
                }
            ]
        };

        var service = new PostService(context);

        // Act
        service.UpsertVoteToPost(_defaultPost.CreatorId, createVoteDto);

        // Assert
        var vote = await context.Votes.FirstOrDefaultAsync();
        Assert.NotNull(vote);
    }

    [Fact]
    public async Task UpsertVoteToPoll_ExistingVote_UpdatesVote()
    {
        // Arrange.
        await using var context = CreateContext();
        var createVoteDto = new UpsertVoteToPostDto()
        {
            PostId = _defaultPost.Id,
            Polls = [
                new UpsertVoteToPollDto
                {
                    PollId = _defaultPost.Polls[0].Id,
                    Candidates = [
                        new UpsertVoteToCandidateDto { CandidateId = _defaultPost.Polls[0].Candidates[0].Id, Value = 1 }
                    ]
                }
            ]
        };

        var service = new PostService(context);
        service.UpsertVoteToPost(_defaultPost.CreatorId, createVoteDto);

        createVoteDto.Polls[0].Candidates[0].Value = 2;

        // Act.
        service.UpsertVoteToPost(_defaultPost.CreatorId, createVoteDto);

        // Assert.
        var vote = await context.Votes.FirstOrDefaultAsync();
        Assert.NotNull(vote);
        Assert.Equal(2, vote.Value);
    }
}
