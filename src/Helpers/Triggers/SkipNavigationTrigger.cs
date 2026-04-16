using Microsoft.EntityFrameworkCore;

public class SkipNavigationTrigger(HiveMimeContext context) : BaseTrigger<Dictionary<string, object>>
{
    public override async Task OnAddedAsync(Dictionary<string, object> entity)
    {
        if (entity.TryGetValue(nameof(User.FollowedHives) + "Id", out var hiveId))
        {
            await context.Hives.Where(h => h.Id == (int)hiveId)
                .ExecuteUpdateAsync(h => h.SetProperty(h => h.FollowerCount, h => h.FollowerCount + 1));
        }
    }

    public override async Task OnDeletedAsync(Dictionary<string, object> entity)
    {
        if (entity.TryGetValue(nameof(User.FollowedHives) + "Id", out var hiveId))
        {
            await context.Hives.Where(h => h.Id == (int)hiveId)
                .ExecuteUpdateAsync(h => h.SetProperty(h => h.FollowerCount, h => h.FollowerCount - 1));
        }
    }
}
