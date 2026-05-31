
using Xunit;

namespace HiveMime.Tests;

public class HotnessUpdateQueueTests
{
    [Fact]
    public void AddPosts_AddsPostsToQueue()
    {
        var queue = new HotnessUpdateQueue();
        var postIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        queue.EnqueuePosts(postIds);
        var result = queue.DequeuePosts(100);

        Assert.Equal(3, result.Count);
        Assert.Contains(postIds[0], result);
        Assert.Contains(postIds[1], result);
        Assert.Contains(postIds[2], result);
    }

    [Fact]
    public void RemovePosts_RemovesPostsFromQueue()
    {
        var queue = new HotnessUpdateQueue();
        var postIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        queue.EnqueuePosts(postIds);

        queue.DequeuePosts(1);
        var result = queue.DequeuePosts(100);

        Assert.Equal(2, result.Count);
        Assert.Contains(postIds[1], result);
        Assert.Contains(postIds[2], result);
        Assert.DoesNotContain(postIds[0], result);
    }

    [Fact]
    public void GetPostsToUpdate_ReturnsAllPosts()
    {
        var queue = new HotnessUpdateQueue();
        var postIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        queue.EnqueuePosts(postIds);

        var result = queue.DequeuePosts(100);
        Assert.Equal(postIds.OrderBy(x => x), result.OrderBy(x => x));
    }
}
