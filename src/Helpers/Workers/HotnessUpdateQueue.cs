using System.Collections.Concurrent;

public class HotnessUpdateQueue
{
    private ConcurrentQueue<Guid> PostQueue = new ConcurrentQueue<Guid>();
    private ConcurrentDictionary<Guid, byte> PostsToUpdate = new ConcurrentDictionary<Guid, byte>();

    public void EnqueuePosts(IEnumerable<Guid> postIds)
    {
        foreach (var postId in postIds)
        {
            if (!PostsToUpdate.ContainsKey(postId))
            {
                PostQueue.Enqueue(postId);
                PostsToUpdate.TryAdd(postId, 0);
            }
        }
    }

    public List<Guid> DequeuePosts(int amount)
    {
        List<Guid> posts = new List<Guid>();

        while (posts.Count < amount && PostQueue.TryDequeue(out Guid postId))
        {
            posts.Add(postId);
            PostsToUpdate.TryRemove(postId, out _);
        }

        return posts;
    }
}
