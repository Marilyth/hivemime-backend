using Microsoft.EntityFrameworkCore;

public class HotnessUpdater : BaseWorker
{
    private HotnessUpdateQueue updateQueue;

    public HotnessUpdater(IServiceScopeFactory serviceScopeFactory, ILogger<HotnessUpdater> logger, HotnessUpdateQueue updateQueue)
        : base(15, serviceScopeFactory, logger)
    {
        this.updateQueue = updateQueue;
    }

    protected override async Task DoWorkAsync(HiveMimeContext context)
    {
        const int batchSize = 1000;

        List<int> postsToUpdate = updateQueue.GetPostsToUpdate();

        for (int i = 0; i < postsToUpdate.Count; i += batchSize)
        {
            var batch = postsToUpdate.GetRange(i, Math.Min(batchSize, postsToUpdate.Count - i));
            await context.Posts.Where(p => batch.Contains(p.Id)).ExecuteUpdateAsync(p => p.UpdateHotness());
            updateQueue.RemovePosts(batch);
        }
    }
}