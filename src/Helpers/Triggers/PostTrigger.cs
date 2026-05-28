using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

public class PostTrigger(HiveMimeContext context) : BaseTrigger<Post>
{
    public override async Task OnAddedAsync(Post entity)
    {
        if (!entity.ApprovalStatus.Equals(ApprovalStatus.Approved))
            return;

        await context.Hives.Where(p => p.Id == entity.HiveId)
            .ExecuteUpdateAsync(p => p.SetProperty(p => p.PostCount, p => p.PostCount + 1));
    }

    public override async Task OnDeletedAsync(Post entity)
    {
        if (!entity.ApprovalStatus.Equals(ApprovalStatus.Approved))
            return;
        
        await context.Hives.Where(p => p.Id == entity.HiveId)
            .ExecuteUpdateAsync(p => p.SetProperty(p => p.PostCount, p => p.PostCount - 1));
    }

    public override async Task OnUpdatedAsync(Post entity, IEnumerable<PropertyEntry> properties)
    {
        // Approval has not changed.
        if (!properties.Any(p => p.Metadata.Name == nameof(Post.ApprovalStatus)))
            return;

        PropertyEntry approvalStatusProperty = properties.First(p => p.Metadata.Name == nameof(Post.ApprovalStatus));

        // Post count has not changed.
        if (!approvalStatusProperty.OriginalValue.Equals(ApprovalStatus.Approved) &&
            !approvalStatusProperty.CurrentValue.Equals(ApprovalStatus.Approved))
            return;

        int change = approvalStatusProperty.CurrentValue.Equals(ApprovalStatus.Approved) ? 1 : -1;
        
        await context.Hives.Where(p => p.Id == entity.HiveId)
            .ExecuteUpdateAsync(p => p.SetProperty(p => p.PostCount, p => p.PostCount + change));
    }
}
