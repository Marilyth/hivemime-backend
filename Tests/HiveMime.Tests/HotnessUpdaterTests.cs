using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace HiveMime.Tests;

public class HotnessUpdaterTests : IntegrationTest
{
    public HotnessUpdaterTests(DatabaseContainer fixture) : base(fixture) { }

    [Fact]
    public async Task DoWorkAsync_UpdatesHotnessAndRemovesFromQueue()
    {
        // Arrange
        var queue = new HotnessUpdateQueue();
        var logger = Mock.Of<ILogger<HotnessUpdater>>();
        var post = Context.Posts.AsNoTracking().First();
        post.HotnessLastRecalculatedAt = DateTimeOffset.UtcNow.AddHours(-2);
        await Context.SaveChangesAsync();

        queue.AddPosts([post.Id]);

        var updater = new HotnessUpdater(Context.GetService<IServiceScopeFactory>(), logger, queue);

        // Act
        await updater.InvokeDoWorkAsync(Context);

        // Assert
        Assert.Empty(queue.GetPostsToUpdate());
        Assert.True(Context.Posts.First().HotnessLastRecalculatedAt > DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    protected override void SeedDatabase()
    {
        Post post = new()
        {
            Title = "Default Post",
            Description = "This is a default post.",
            Creator = new User { Username = "defaultuser", Settings = new() },
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

        Context.Posts.Add(post);
    }
}

// Extension to invoke protected DoWorkAsync for testing
public static class HotnessUpdaterTestExtensions
{
    public static async Task InvokeDoWorkAsync(this HotnessUpdater updater, HiveMimeContext context)
    {
        var method = typeof(HotnessUpdater).GetMethod("DoWorkAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method.Invoke(updater, new object[] { context });
    }
}
