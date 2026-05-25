using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

public abstract class BaseTrigger<T> : ITrigger
{
    public virtual Task OnAddingAsync(T entity)
        => Task.CompletedTask;

    public virtual Task OnAddedAsync(T entity)
        => Task.CompletedTask;

    public virtual Task OnUpdatingAsync(T entity, IEnumerable<PropertyEntry> properties)
        => Task.CompletedTask;

    public virtual Task OnUpdatedAsync(T entity, IEnumerable<PropertyEntry> properties)
        => Task.CompletedTask;

    public virtual Task OnDeletingAsync(T entity)
        => Task.CompletedTask;

    public virtual Task OnDeletedAsync(T entity)
        => Task.CompletedTask;

    public async Task ExecuteAsync(TriggerDispatcher.TriggerEntry entry, bool isBefore = false)
    {
        if (entry.Entity is not T typedEntity)
            throw new ArgumentException($"Invalid entity type for {GetType().Name}.");

        switch (entry.State)
        {
            case EntityState.Added:
                if (isBefore)
                    await OnAddingAsync(typedEntity);
                else
                    await OnAddedAsync(typedEntity);
                break;
            case EntityState.Modified:
                if (isBefore)
                    await OnUpdatingAsync(typedEntity, entry.Properties);
                else
                    await OnUpdatedAsync(typedEntity, entry.Properties);
                break;
            case EntityState.Deleted:
                if (isBefore)
                    await OnDeletingAsync(typedEntity);
                else
                    await OnDeletedAsync(typedEntity);
                break;
        }
    }

    public bool CanHandle(Type entityType)
        => typeof(T).IsAssignableFrom(entityType);
}

public interface ITrigger
{
    Task ExecuteAsync(TriggerDispatcher.TriggerEntry entry, bool isBefore = false);
    bool CanHandle(Type entityType);
}
