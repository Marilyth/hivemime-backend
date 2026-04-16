using Microsoft.EntityFrameworkCore;

public abstract class BaseTrigger<T> : ITrigger
{
    public virtual Task OnAddingAsync(T entity)
        => Task.CompletedTask;

    public virtual Task OnAddedAsync(T entity)
        => Task.CompletedTask;

    public virtual Task OnUpdatingAsync(T entity)
        => Task.CompletedTask;

    public virtual Task OnUpdatedAsync(T entity)
        => Task.CompletedTask;

    public virtual Task OnDeletingAsync(T entity)
        => Task.CompletedTask;

    public virtual Task OnDeletedAsync(T entity)
        => Task.CompletedTask;

    public async Task ExecuteAsync(object entity, EntityState state, bool isBefore = false)
    {
        if (entity is not T typedEntity)
            throw new ArgumentException($"Invalid entity type for {GetType().Name}.");

        switch (state)
        {
            case EntityState.Added:
                if (isBefore)
                    await OnAddingAsync(typedEntity);
                else
                    await OnAddedAsync(typedEntity);
                break;
            case EntityState.Modified:
                if (isBefore)
                    await OnUpdatingAsync(typedEntity);
                else
                    await OnUpdatedAsync(typedEntity);
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
    Task ExecuteAsync(object entity, EntityState state, bool isBefore = false);
    bool CanHandle(Type entityType);
}
