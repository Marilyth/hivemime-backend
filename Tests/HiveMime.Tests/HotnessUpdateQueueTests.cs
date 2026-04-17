
using Xunit;

namespace HiveMime.Tests;

public class HotnessUpdateQueueTests
{
    [Fact]
    public void AddPosts_AddsPostsToQueue()
    {
        var queue = new HotnessUpdateQueue();
        var postIds = new List<int> { 1, 2, 3 };

        queue.EnqueuePosts(postIds);
        var result = queue.DequeuePosts(100);

        Assert.Equal(3, result.Count);
        Assert.Contains(1, result);
        Assert.Contains(2, result);
        Assert.Contains(3, result);
    }

    [Fact]
    public void RemovePosts_RemovesPostsFromQueue()
    {
        var queue = new HotnessUpdateQueue();
        var postIds = new List<int> { 1, 2, 3 };
        queue.EnqueuePosts(postIds);

        queue.DequeuePosts(1);
        var result = queue.DequeuePosts(100);

        Assert.Equal(2, result.Count);
        Assert.Contains(2, result);
        Assert.Contains(3, result);
        Assert.DoesNotContain(1, result);
    }

    [Fact]
    public void GetPostsToUpdate_ReturnsAllPosts()
    {
        var queue = new HotnessUpdateQueue();
        var postIds = new List<int> { 10, 20 };
        queue.EnqueuePosts(postIds);

        var result = queue.DequeuePosts(100);
        Assert.Equal(postIds.OrderBy(x => x), result.OrderBy(x => x));
    }
}
