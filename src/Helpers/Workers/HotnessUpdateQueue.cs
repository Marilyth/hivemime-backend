using System.Collections.Concurrent;

public class HotnessUpdateQueue
{
    private ConcurrentQueue<int> PostQueue = new ConcurrentQueue<int>();
    private ConcurrentDictionary<int, byte> PostsToUpdate = new ConcurrentDictionary<int, byte>();

    public void EnqueuePosts(IEnumerable<int> postIds)
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

    public List<int> DequeuePosts(int amount)
    {
        List<int> posts = new List<int>();

        while (posts.Count < amount && PostQueue.TryDequeue(out int postId))
        {
            posts.Add(postId);
            PostsToUpdate.TryRemove(postId, out _);
        }

        return posts;
    }
}
