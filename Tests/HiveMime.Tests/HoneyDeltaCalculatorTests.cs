using Microsoft.EntityFrameworkCore.Infrastructure;

namespace HiveMime.Tests;

public class HoneyDeltaCalculatorTests : IntegrationTest
{
    private readonly HoneyDeltaCalculator _calculator;

    public HoneyDeltaCalculatorTests(DatabaseContainer fixture) : base(fixture)
    {
        _calculator = Context.GetService<HoneyDeltaCalculator>();
    }

    [Fact]
    public async Task FromCommentDtoAsync_CalculatesHoneyDelta()
    {
        // Arrange
        var user = new User { Username = "testuser", Honey = 0, Settings = new() };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();
        var dto = new CommentDto { Content = new string('a', 256), User = new UserDto { Id = user.Id } };

        // Act
        var result = await _calculator.FromCommentDtoAsync(user.Id, dto);

        // Assert
        Context.ChangeTracker.Clear();
        var updatedUser = await Context.Users.FindAsync(user.Id);

        Assert.True(result.HoneyDelta > 0);
        Assert.Equal(dto.Content, result.Dto.Content);
        Assert.True(updatedUser.Honey > 0);
    }


    [Fact]
    public async Task FromPostDtoAsync_CalculatesHoneyDelta()
    {
        // Arrange
        var user = new User { Username = "testuser2", Honey = 0, Settings = new() };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();
        var poll = new PollDto { Candidates = [ new CandidateDto() ], Categories = [ new CategoryDto() ], Description = new string('b', 128) };
        var dto = new PostDto { Polls = [ poll ] };

        // Act
        var result = await _calculator.FromPostDtoAsync(user.Id, dto);

        // Assert
        Context.ChangeTracker.Clear();
        var updatedUser = await Context.Users.FindAsync(user.Id);

        Assert.True(result.HoneyDelta > 0);
        Assert.Equal(dto.Polls.Count, result.Dto.Polls.Count);
        Assert.True(updatedUser.Honey > 0);
    }

    [Fact]
    public async Task FromPostVoteAsync_CalculatesHoneyDelta()
    {
        // Arrange
        var user = new User { Username = "testuser3", Honey = 0, Settings = new() };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();
        var pollVote = new PollVoteDto { Candidates = [ new CandidateChoiceVoteDto() ] };
        var dto = new PostVoteDto { Polls = [ pollVote ] };

        // Act
        var result = await _calculator.FromPostVoteAsync(user.Id, dto);

        // Assert
        Context.ChangeTracker.Clear();
        var updatedUser = await Context.Users.FindAsync(user.Id);

        Assert.True(result.HoneyDelta > 0);
        Assert.True(result.Dto);
        Assert.True(updatedUser.Honey > 0);
    }

    [Fact]
    public async Task AwardScoreAsync_DecaysHoneyWithMultipleAwards()
    {
        // Arrange
        var user = new User { Username = "testuser4", Honey = 0, Settings = new() };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();
        var dto = new CommentDto { Content = new string('a', 128), User = new UserDto { Id = user.Id } };
        double lastDelta = 0;

        // Act
        for (int i = 0; i < 5; i++)
        {
            var result = await _calculator.FromCommentDtoAsync(user.Id, dto);
            if (i > 0)
                Assert.True(result.HoneyDelta < lastDelta);
            lastDelta = result.HoneyDelta;
        }

        // Assert
        Context.ChangeTracker.Clear();
        var updatedUser = await Context.Users.FindAsync(user.Id);
        
        Assert.True(updatedUser.Honey > 0);
    }
}
