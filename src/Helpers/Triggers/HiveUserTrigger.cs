using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

public class HiveUserTrigger(HiveMimeContext context) : BaseTrigger<HiveUser>
{
    public override async Task OnAddedAsync(HiveUser entity)
    {
        if (!entity.ApprovalStatus.Equals(ApprovalStatus.Approved))
            return;

        await context.Hives.Where(h => h.Id == entity.HiveId)
            .ExecuteUpdateAsync(h => h.SetProperty(h => h.UserCount, h => h.UserCount + 1));
    }

    public override async Task OnDeletedAsync(HiveUser entity)
    {
        if (!entity.ApprovalStatus.Equals(ApprovalStatus.Approved))
            return;

        await context.Hives.Where(h => h.Id == entity.HiveId)
            .ExecuteUpdateAsync(h => h.SetProperty(h => h.UserCount, h => h.UserCount - 1));
    }

    public override async Task OnUpdatedAsync(HiveUser entity, IEnumerable<PropertyEntry> properties)
    {
        // Approval has not changed.
        if (!properties.Any(p => p.Metadata.Name == nameof(HiveUser.ApprovalStatus)))
            return;

        // User count has not changed.
        if (entity.ApprovalStatus.Equals(ApprovalStatus.Rejected))
            return;

        bool isApproved = entity.ApprovalStatus.Equals(ApprovalStatus.Approved);
        int hiveId = entity.HiveId;
        int change = isApproved ? 1 : -1;
        
        await context.Hives.Where(h => h.Id == hiveId)
            .ExecuteUpdateAsync(h => h.SetProperty(h => h.UserCount, h => h.UserCount + change));
    }
}
