using Microsoft.EntityFrameworkCore;

public class HotnessUpdater : BaseWorker
{
    private HotnessUpdateQueue updateQueue;

    public HotnessUpdater(IServiceScopeFactory serviceScopeFactory, ILogger<HotnessUpdater> logger, HotnessUpdateQueue updateQueue)
        : base(1, serviceScopeFactory, logger)
    {
        this.updateQueue = updateQueue;
    }

    protected override async Task DoWorkAsync(HiveMimeContext context)
    {
        const int batchSize = 1000;

        List<Guid> postsToUpdate = updateQueue.DequeuePosts(batchSize);

        if (postsToUpdate.Count == 0)
            return;
            
        await context.Posts.Where(p => postsToUpdate.Contains(p.Id)).ExecuteUpdateAsync(p => p.UpdateHotness());
    }
}