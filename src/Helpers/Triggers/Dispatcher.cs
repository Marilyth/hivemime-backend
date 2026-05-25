using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

public class TriggerDispatcher(IEnumerable<ITrigger> triggers)
{
    private List<TriggerEntry> entries;

    public void RegisterChanges(ChangeTracker changeTracker)
    {
        entries = changeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .Select(e => new TriggerEntry(e.Entity, e.State, e.Properties.Where(p => p.IsModified)))
            .ToList();
    }

    public async Task DispatchAsync(bool isBeforeSave)
    {
        foreach (var entry in entries)
        {
            var entity = entry.Entity;
            var entityType = entity.GetType();
            var state = entry.State;

            foreach (var trigger in triggers.Where(t => t.CanHandle(entityType)))
                await trigger.ExecuteAsync(entry, isBeforeSave);
        }
    }

    public record TriggerEntry(object Entity, EntityState State, IEnumerable<PropertyEntry> Properties);
}