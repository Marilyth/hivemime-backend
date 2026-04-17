
using Xunit;

namespace HiveMime.Tests;

public class HotnessUpdateQueueTests
{
    [Fact]
    public void AddPosts_AddsPostsToQueue()
    {
        var queue = new HotnessUpdateQueue();
        var postIds = new List<int> { 1, 2, 3 };

        queue.AddPosts(postIds);
        var result = queue.GetPostsToUpdate();

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
        queue.AddPosts(postIds);

        queue.RemovePosts(new List<int> { 2 });
        var result = queue.GetPostsToUpdate();

        Assert.Equal(2, result.Count);
        Assert.Contains(1, result);
        Assert.Contains(3, result);
        Assert.DoesNotContain(2, result);
    }

    [Fact]
    public void GetPostsToUpdate_ReturnsAllPosts()
    {
        var queue = new HotnessUpdateQueue();
        var postIds = new List<int> { 10, 20 };
        queue.AddPosts(postIds);

        var result = queue.GetPostsToUpdate();
        Assert.Equal(postIds.OrderBy(x => x), result.OrderBy(x => x));
    }
}
