using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

public interface IHasIdentifier
{
    Guid Id { get; set; }
}

public abstract class EntityWithIdentifier : Entity, IHasIdentifier
{
    [Key]
    public Guid Id { get; set; }
}

[Index(nameof(CreatedAt))]
public abstract class Entity
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAt { get; set; }
}