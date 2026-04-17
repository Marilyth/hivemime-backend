using System.Collections.Concurrent;

public class HotnessUpdateQueue
{
    private ConcurrentDictionary<int, byte> PostsToUpdate = new ConcurrentDictionary<int, byte>();

    public void AddPosts(IEnumerable<int> postIds)
    {
        foreach (var postId in postIds)
            PostsToUpdate.TryAdd(postId, 0);
    }

    public void RemovePosts(IEnumerable<int> postIds)
    {
        foreach (var postId in postIds)
            PostsToUpdate.TryRemove(postId, out _);
    }

    public List<int> GetPostsToUpdate()
    {
        return PostsToUpdate.Keys.ToList();
    }
}
