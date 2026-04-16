using Microsoft.EntityFrameworkCore;

public class PostTrigger(HiveMimeContext context) : BaseTrigger<Post>
{
    public override async Task OnAddedAsync(Post entity)
        => await context.Hives.Where(p => p.Id == entity.HiveId)
            .ExecuteUpdateAsync(p => p.SetProperty(p => p.PostCount, p => p.PostCount + 1));

    public override async Task OnDeletedAsync(Post entity)
        => await context.Hives.Where(p => p.Id == entity.HiveId)
            .ExecuteUpdateAsync(p => p.SetProperty(p => p.PostCount, p => p.PostCount - 1));
}
